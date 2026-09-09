using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Notifications.Interfaces;
using CivisOS.Application.Tasks.DTOs;
using CivisOS.Application.Tasks.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using CivisOS.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class WorkTaskService : IWorkTaskService
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public WorkTaskService(IApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<ApiResponse<PagedResult<WorkTaskDto>>> GetTasksAsync(
        WorkTaskListQuery query,
        CancellationToken cancellationToken = default)
    {
        await MarkOverdueAsync(cancellationToken);
        return await QueryAsync(query, assigneeOverride: null, cancellationToken);
    }

    public async Task<ApiResponse<WorkTaskDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await MarkOverdueAsync(cancellationToken);
        var dto = await MapByIdAsync(id, cancellationToken);
        return dto is null
            ? ApiResponse<WorkTaskDto>.Fail("Task not found.")
            : ApiResponse<WorkTaskDto>.Ok(dto);
    }

    public async Task<ApiResponse<WorkTaskDto>> CreateAsync(
        string actorUserId,
        CreateWorkTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == actorUserId, cancellationToken);

        if (request.GeoFenceId.HasValue
            && !await _db.GeoFences.AnyAsync(f => f.Id == request.GeoFenceId.Value, cancellationToken))
        {
            return ApiResponse<WorkTaskDto>.Fail("Geo-fence not found.");
        }

        if (request.CleaningAreaId.HasValue)
        {
            var area = await _db.CleaningAreas.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.CleaningAreaId.Value, cancellationToken);
            if (area is null)
            {
                return ApiResponse<WorkTaskDto>.Fail("Cleaning area not found.");
            }

            if (!request.GeoFenceId.HasValue && area.GeoFenceId.HasValue)
            {
                request = request with { GeoFenceId = area.GeoFenceId };
            }
        }

        if (request.AssigneeEmployeeId.HasValue
            && !await _db.Employees.AnyAsync(e => e.Id == request.AssigneeEmployeeId.Value && e.IsActive, cancellationToken))
        {
            return ApiResponse<WorkTaskDto>.Fail("Assignee employee not found or inactive.");
        }

        var task = new WorkTask
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            Priority = request.Priority,
            DeadlineUtc = request.DeadlineUtc?.ToUniversalTime(),
            AssigneeEmployeeId = request.AssigneeEmployeeId,
            AssignedByEmployeeId = actor?.Id,
            GeoFenceId = request.GeoFenceId,
            CleaningAreaId = request.CleaningAreaId,
            Status = WorkTaskStatus.Pending
        };

        _db.Add(task);
        await _db.SaveChangesAsync(cancellationToken);

        if (task.AssigneeEmployeeId.HasValue)
        {
            await NotifyAssigneeAsync(task, cancellationToken);
        }

        return ApiResponse<WorkTaskDto>.Ok(await MapByIdAsync(task.Id, cancellationToken), "Task created.");
    }

    public async Task<ApiResponse<WorkTaskDto>> UpdateAsync(
        Guid id,
        UpdateWorkTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _db.WorkTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return ApiResponse<WorkTaskDto>.Fail("Task not found.");
        }

        if (request.GeoFenceId.HasValue
            && !await _db.GeoFences.AnyAsync(f => f.Id == request.GeoFenceId.Value, cancellationToken))
        {
            return ApiResponse<WorkTaskDto>.Fail("Geo-fence not found.");
        }

        if (request.CleaningAreaId.HasValue
            && !await _db.CleaningAreas.AnyAsync(a => a.Id == request.CleaningAreaId.Value, cancellationToken))
        {
            return ApiResponse<WorkTaskDto>.Fail("Cleaning area not found.");
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.Priority = request.Priority;
        task.DeadlineUtc = request.DeadlineUtc?.ToUniversalTime();
        task.GeoFenceId = request.GeoFenceId;
        task.CleaningAreaId = request.CleaningAreaId;
        task.UpdatedAtUtc = DateTime.UtcNow;

        _db.Update(task);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<WorkTaskDto>.Ok(await MapByIdAsync(task.Id, cancellationToken), "Task updated.");
    }

    public async Task<ApiResponse<WorkTaskDto>> AssignAsync(
        string actorUserId,
        Guid id,
        AssignWorkTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _db.WorkTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return ApiResponse<WorkTaskDto>.Fail("Task not found.");
        }

        var assignee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.AssigneeEmployeeId && e.IsActive, cancellationToken);
        if (assignee is null)
        {
            return ApiResponse<WorkTaskDto>.Fail("Assignee employee not found or inactive.");
        }

        var actor = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == actorUserId, cancellationToken);

        task.AssigneeEmployeeId = assignee.Id;
        task.AssignedByEmployeeId = actor?.Id;
        task.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(task);
        await _db.SaveChangesAsync(cancellationToken);

        await NotifyAssigneeAsync(task, cancellationToken);
        return ApiResponse<WorkTaskDto>.Ok(await MapByIdAsync(task.Id, cancellationToken), "Task assigned.");
    }

    public async Task<ApiResponse<WorkTaskDto>> UpdateStatusAsync(
        Guid id,
        UpdateWorkTaskStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _db.WorkTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return ApiResponse<WorkTaskDto>.Fail("Task not found.");
        }

        if (request.Status == WorkTaskStatus.InProgress && task.Status == WorkTaskStatus.Pending)
        {
            return ApiResponse<WorkTaskDto>.Fail("Use the start endpoint to begin a pending task (GPS required when a fence is linked).");
        }

        task.Status = request.Status;
        task.StatusNotes = request.Notes;
        task.UpdatedAtUtc = DateTime.UtcNow;

        if (request.Status == WorkTaskStatus.Completed)
        {
            task.CompletedAtUtc = DateTime.UtcNow;
        }

        _db.Update(task);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<WorkTaskDto>.Ok(await MapByIdAsync(task.Id, cancellationToken), "Task status updated.");
    }

    public async Task<ApiResponse<WorkTaskDto>> StartAsync(
        string userId,
        Guid id,
        StartWorkTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);
        if (employee is null)
        {
            return ApiResponse<WorkTaskDto>.Fail("No employee profile is linked to this user account.");
        }

        var task = await _db.WorkTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return ApiResponse<WorkTaskDto>.Fail("Task not found.");
        }

        if (task.AssigneeEmployeeId != employee.Id)
        {
            return ApiResponse<WorkTaskDto>.Fail("Only the assigned employee can start this task.");
        }

        if (task.Status is not WorkTaskStatus.Pending and not WorkTaskStatus.Overdue)
        {
            return ApiResponse<WorkTaskDto>.Fail("Only pending or overdue tasks can be started.");
        }

        if (task.GeoFenceId.HasValue)
        {
            var fence = await _db.GeoFences.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == task.GeoFenceId.Value, cancellationToken);
            if (fence is null)
            {
                return ApiResponse<WorkTaskDto>.Fail("Linked geo-fence was not found.");
            }

            if (fence.Status != GeoFenceStatus.Active)
            {
                return ApiResponse<WorkTaskDto>.Fail("Linked geo-fence is inactive.");
            }

            if (!GeoMath.IsInsideFence(fence, request.Latitude, request.Longitude))
            {
                var distance = Math.Round(
                    GeoMath.DistanceMeters(
                        fence.CenterLatitude,
                        fence.CenterLongitude,
                        request.Latitude,
                        request.Longitude),
                    2);
                return ApiResponse<WorkTaskDto>.Fail(
                    "Cannot start task: you are outside the linked geo-fence.",
                    new[]
                    {
                        $"Outside geo-fence '{fence.Name}' (distance {distance} m, radius {fence.RadiusMeters} m)."
                    });
            }
        }

        task.Status = WorkTaskStatus.InProgress;
        task.StartedAtUtc = DateTime.UtcNow;
        task.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(task);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<WorkTaskDto>.Ok(await MapByIdAsync(task.Id, cancellationToken), "Task started.");
    }

    public async Task<ApiResponse<PagedResult<WorkTaskDto>>> GetMyTasksAsync(
        string userId,
        WorkTaskListQuery query,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);
        if (employee is null)
        {
            return ApiResponse<PagedResult<WorkTaskDto>>.Fail("No employee profile is linked to this user account.");
        }

        await MarkOverdueAsync(cancellationToken);
        return await QueryAsync(query, employee.Id, cancellationToken);
    }

    public async Task<ApiResponse<WorkTaskDto>> ApproveAsync(
        string actorUserId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var task = await _db.WorkTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return ApiResponse<WorkTaskDto>.Fail("Task not found.");
        }

        if (task.Status != WorkTaskStatus.Completed)
        {
            return ApiResponse<WorkTaskDto>.Fail("Only completed tasks can be approved.");
        }

        var actor = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == actorUserId, cancellationToken);

        task.ApprovedAtUtc = DateTime.UtcNow;
        task.ApprovedByEmployeeId = actor?.Id;
        task.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(task);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<WorkTaskDto>.Ok(await MapByIdAsync(task.Id, cancellationToken), "Task approved.");
    }

    private async Task NotifyAssigneeAsync(WorkTask task, CancellationToken cancellationToken)
    {
        if (!task.AssigneeEmployeeId.HasValue)
        {
            return;
        }

        var assignee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == task.AssigneeEmployeeId.Value, cancellationToken);
        if (assignee?.UserId is null)
        {
            return;
        }

        await _notifications.NotifyUserAsync(
            assignee.UserId,
            "Task assigned",
            $"You have been assigned: {task.Title}",
            NotificationType.TaskAssigned,
            task.Id.ToString(),
            "WorkTask",
            cancellationToken);
    }

    private async Task MarkOverdueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var overdue = await _db.WorkTasks
            .Where(t => t.DeadlineUtc != null
                        && t.DeadlineUtc < now
                        && (t.Status == WorkTaskStatus.Pending || t.Status == WorkTaskStatus.InProgress))
            .ToListAsync(cancellationToken);

        if (overdue.Count == 0)
        {
            return;
        }

        foreach (var task in overdue)
        {
            task.Status = WorkTaskStatus.Overdue;
            task.UpdatedAtUtc = now;
            _db.Update(task);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApiResponse<PagedResult<WorkTaskDto>>> QueryAsync(
        WorkTaskListQuery query,
        Guid? assigneeOverride,
        CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;
        var assigneeId = assigneeOverride ?? query.AssigneeEmployeeId;

        var rows = _db.WorkTasks.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            rows = rows.Where(t => t.Title.Contains(term) || (t.Description != null && t.Description.Contains(term)));
        }

        if (query.Status.HasValue)
        {
            rows = rows.Where(t => t.Status == query.Status.Value);
        }

        if (assigneeId.HasValue)
        {
            rows = rows.Where(t => t.AssigneeEmployeeId == assigneeId.Value);
        }

        if (query.Priority.HasValue)
        {
            rows = rows.Where(t => t.Priority == query.Priority.Value);
        }

        var total = await rows.CountAsync(cancellationToken);
        var ids = await rows
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var items = new List<WorkTaskDto>();
        foreach (var id in ids)
        {
            var dto = await MapByIdAsync(id, cancellationToken);
            if (dto is not null)
            {
                items.Add(dto);
            }
        }

        return ApiResponse<PagedResult<WorkTaskDto>>.Ok(
            PagedResult<WorkTaskDto>.Create(items, total, pageNumber, pageSize));
    }

    private async Task<WorkTaskDto?> MapByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.WorkTasks.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new WorkTaskDto(
                t.Id,
                t.Title,
                t.Description,
                t.Priority,
                t.Status,
                t.DeadlineUtc,
                t.AssigneeEmployeeId,
                t.AssigneeEmployee == null ? null : t.AssigneeEmployee.FirstName + " " + t.AssigneeEmployee.LastName,
                t.AssignedByEmployeeId,
                t.AssignedByEmployee == null ? null : t.AssignedByEmployee.FirstName + " " + t.AssignedByEmployee.LastName,
                t.GeoFenceId,
                t.GeoFence == null ? null : t.GeoFence.Name,
                t.CleaningAreaId,
                t.CleaningArea == null ? null : t.CleaningArea.Name,
                t.StartedAtUtc,
                t.CompletedAtUtc,
                t.ApprovedAtUtc,
                t.StatusNotes,
                t.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
