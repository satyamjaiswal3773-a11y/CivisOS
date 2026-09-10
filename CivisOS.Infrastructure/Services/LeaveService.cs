using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Leaves.DTOs;
using CivisOS.Application.Leaves.Interfaces;
using CivisOS.Application.Notifications.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class LeaveService : ILeaveService
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public LeaveService(IApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<ApiResponse<IReadOnlyList<LeaveTypeDto>>> GetLeaveTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _db.LeaveTypes.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LeaveTypeDto(x.Id, x.Code, x.Name, x.IsPaid, x.IsActive, x.Description))
            .ToListAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<LeaveTypeDto>>.Ok(items);
    }

    public async Task<ApiResponse<LeaveTypeDto>> CreateLeaveTypeAsync(
        CreateLeaveTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await _db.LeaveTypes.AnyAsync(x => x.Code == request.Code, cancellationToken))
            return ApiResponse<LeaveTypeDto>.Fail("Leave type code already exists.");

        var entity = new LeaveType
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            IsPaid = request.IsPaid,
            Description = request.Description
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<LeaveTypeDto>.Ok(MapType(entity), "Leave type created.");
    }

    public async Task<ApiResponse<LeaveTypeDto>> UpdateLeaveTypeAsync(
        Guid id,
        UpdateLeaveTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.LeaveTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return ApiResponse<LeaveTypeDto>.Fail("Leave type not found.");

        entity.Name = request.Name.Trim();
        entity.IsPaid = request.IsPaid;
        entity.IsActive = request.IsActive;
        entity.Description = request.Description;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<LeaveTypeDto>.Ok(MapType(entity), "Leave type updated.");
    }

    public async Task<ApiResponse<LeaveRequestDto>> SubmitAsync(
        string userId,
        CreateLeaveRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var employeeId = request.EmployeeId;
        if (!employeeId.HasValue)
        {
            var self = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
            if (self is null) return ApiResponse<LeaveRequestDto>.Fail("Employee profile not found.");
            employeeId = self.Id;
        }

        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == employeeId && x.IsActive, cancellationToken);
        if (employee is null) return ApiResponse<LeaveRequestDto>.Fail("Employee not found or inactive.");

        var leaveType = await _db.LeaveTypes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.LeaveTypeId && x.IsActive, cancellationToken);
        if (leaveType is null) return ApiResponse<LeaveRequestDto>.Fail("Active leave type not found.");

        if (request.ToDate < request.FromDate)
            return ApiResponse<LeaveRequestDto>.Fail("ToDate cannot be earlier than FromDate.");

        var totalDays = request.IsHalfDay
            ? 0.5m
            : (decimal)(request.ToDate.DayNumber - request.FromDate.DayNumber + 1);

        var entity = new LeaveRequest
        {
            EmployeeId = employeeId.Value,
            LeaveTypeId = request.LeaveTypeId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            TotalDays = totalDays,
            IsHalfDay = request.IsHalfDay,
            Reason = request.Reason.Trim(),
            RequestedByUserId = userId,
            Status = LeaveRequestStatus.Pending
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        if (employee.SupervisorId.HasValue)
        {
            var supervisor = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == employee.SupervisorId, cancellationToken);
            if (!string.IsNullOrEmpty(supervisor?.UserId))
            {
                await _notifications.NotifyUserAsync(
                    supervisor.UserId,
                    "Leave submitted",
                    $"{employee.FirstName} {employee.LastName} submitted a leave request.",
                    NotificationType.LeaveSubmitted,
                    entity.Id.ToString(),
                    "LeaveRequest",
                    cancellationToken);
            }
        }

        return ApiResponse<LeaveRequestDto>.Ok(await MapRequestAsync(entity.Id, cancellationToken)!, "Leave request submitted.");
    }

    public async Task<ApiResponse<PagedResult<LeaveRequestDto>>> GetAsync(
        LeaveListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q =
            from lr in _db.LeaveRequests.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on lr.EmployeeId equals e.Id
            join lt in _db.LeaveTypes.AsNoTracking() on lr.LeaveTypeId equals lt.Id
            select new { lr, e, lt };

        if (query.EmployeeId.HasValue) q = q.Where(x => x.lr.EmployeeId == query.EmployeeId);
        if (query.Status.HasValue) q = q.Where(x => x.lr.Status == query.Status);
        if (query.From.HasValue) q = q.Where(x => x.lr.ToDate >= query.From);
        if (query.To.HasValue) q = q.Where(x => x.lr.FromDate <= query.To);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(x => x.lr.CreatedAtUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new LeaveRequestDto(
                x.lr.Id, x.lr.EmployeeId, x.e.EmployeeCode, x.e.FirstName + " " + x.e.LastName,
                x.lr.LeaveTypeId, x.lt.Name, x.lr.FromDate, x.lr.ToDate, x.lr.TotalDays,
                x.lr.IsHalfDay, x.lr.Reason, x.lr.Status, x.lr.ApproverRemarks, x.lr.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<LeaveRequestDto>>.Ok(
            PagedResult<LeaveRequestDto>.Create(items, total, query.PageNumber, query.PageSize));
    }

    public async Task<ApiResponse<LeaveRequestDto>> ApproveAsync(
        string approverUserId,
        Guid id,
        LeaveApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        LeaveRequest? entity = null;
        try
        {
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                entity = await _db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (entity is null)
                    throw new InvalidOperationException("Leave request not found.");
                if (entity.Status is not (LeaveRequestStatus.Pending))
                    throw new InvalidOperationException("Only pending leave requests can be approved.");

                entity.Status = LeaveRequestStatus.Approved;
                entity.ApprovedByUserId = approverUserId;
                entity.ApprovedAtUtc = DateTime.UtcNow;
                entity.ApproverRemarks = request.Remarks;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                _db.Update(entity);
                await _db.SaveChangesAsync(ct);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<LeaveRequestDto>.Fail(ex.Message);
        }

        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity!.EmployeeId, cancellationToken);
        if (!string.IsNullOrEmpty(employee?.UserId))
        {
            await _notifications.NotifyUserAsync(
                employee.UserId,
                "Leave approved",
                $"Your leave from {entity!.FromDate:yyyy-MM-dd} to {entity.ToDate:yyyy-MM-dd} was approved.",
                NotificationType.LeaveApproved,
                entity.Id.ToString(),
                "LeaveRequest",
                cancellationToken);
        }

        return ApiResponse<LeaveRequestDto>.Ok(await MapRequestAsync(id, cancellationToken)!, "Leave approved.");
    }

    public async Task<ApiResponse<LeaveRequestDto>> RejectAsync(
        string approverUserId,
        Guid id,
        LeaveApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        LeaveRequest? entity = null;
        try
        {
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                entity = await _db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (entity is null)
                    throw new InvalidOperationException("Leave request not found.");
                if (entity.Status is not LeaveRequestStatus.Pending)
                    throw new InvalidOperationException("Only pending leave requests can be rejected.");

                entity.Status = LeaveRequestStatus.Rejected;
                entity.RejectedByUserId = approverUserId;
                entity.RejectedAtUtc = DateTime.UtcNow;
                entity.ApproverRemarks = request.Remarks;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                _db.Update(entity);
                await _db.SaveChangesAsync(ct);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<LeaveRequestDto>.Fail(ex.Message);
        }

        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity!.EmployeeId, cancellationToken);
        if (!string.IsNullOrEmpty(employee?.UserId))
        {
            await _notifications.NotifyUserAsync(
                employee.UserId,
                "Leave rejected",
                $"Your leave from {entity!.FromDate:yyyy-MM-dd} to {entity.ToDate:yyyy-MM-dd} was rejected.",
                NotificationType.LeaveRejected,
                entity.Id.ToString(),
                "LeaveRequest",
                cancellationToken);
        }

        return ApiResponse<LeaveRequestDto>.Ok(await MapRequestAsync(id, cancellationToken)!, "Leave rejected.");
    }

    public async Task<bool> HasApprovedLeaveAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _db.LeaveRequests.AsNoTracking().AnyAsync(x =>
            x.EmployeeId == employeeId
            && x.Status == LeaveRequestStatus.Approved
            && x.FromDate <= date
            && x.ToDate >= date, cancellationToken);
    }

    private async Task<LeaveRequestDto?> MapRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        return await (
            from lr in _db.LeaveRequests.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on lr.EmployeeId equals e.Id
            join lt in _db.LeaveTypes.AsNoTracking() on lr.LeaveTypeId equals lt.Id
            where lr.Id == id
            select new LeaveRequestDto(
                lr.Id, lr.EmployeeId, e.EmployeeCode, e.FirstName + " " + e.LastName,
                lr.LeaveTypeId, lt.Name, lr.FromDate, lr.ToDate, lr.TotalDays,
                lr.IsHalfDay, lr.Reason, lr.Status, lr.ApproverRemarks, lr.CreatedAtUtc)
        ).FirstOrDefaultAsync(cancellationToken);
    }

    private static LeaveTypeDto MapType(LeaveType x) =>
        new(x.Id, x.Code, x.Name, x.IsPaid, x.IsActive, x.Description);
}
