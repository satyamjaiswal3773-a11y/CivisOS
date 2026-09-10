using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Notifications.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class OvertimeService : IOvertimeService
{
    private readonly IApplicationDbContext _db;
    private readonly IAttendanceLockService _lockService;
    private readonly IAttendanceAuditService _audit;
    private readonly INotificationService _notifications;

    public OvertimeService(
        IApplicationDbContext db,
        IAttendanceLockService lockService,
        IAttendanceAuditService audit,
        INotificationService notifications)
    {
        _db = db;
        _lockService = lockService;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task<ApiResponse<OvertimeRequestDto>> SubmitAsync(
        string userId,
        CreateOvertimeRequest request,
        CancellationToken cancellationToken = default)
    {
        var employeeId = request.EmployeeId;
        if (!employeeId.HasValue)
        {
            var self = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
            if (self is null) return ApiResponse<OvertimeRequestDto>.Fail("Employee profile not found.");
            employeeId = self.Id;
        }

        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == employeeId && x.IsActive, cancellationToken);
        if (employee is null) return ApiResponse<OvertimeRequestDto>.Fail("Employee not found or inactive.");

        if (await _lockService.IsMonthLockedAsync(request.OvertimeDate.Year, request.OvertimeDate.Month, cancellationToken))
            return ApiResponse<OvertimeRequestDto>.Fail("Attendance is locked for this month.");

        if (request.RequestedHours <= 0)
            return ApiResponse<OvertimeRequestDto>.Fail("Requested hours must be greater than zero.");

        if (string.IsNullOrWhiteSpace(request.Reason))
            return ApiResponse<OvertimeRequestDto>.Fail("Reason is required.");

        var entity = new OvertimeRequest
        {
            EmployeeId = employeeId.Value,
            OvertimeDate = request.OvertimeDate,
            RequestedHours = request.RequestedHours,
            Reason = request.Reason.Trim(),
            Remarks = request.Remarks,
            RequestedByUserId = userId,
            Status = AttendanceApprovalStatus.Pending
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AttendanceAuditAction.OvertimeSubmitted,
            userId,
            employeeId,
            request.OvertimeDate,
            null,
            $"{request.RequestedHours}h",
            request.Reason,
            null,
            "OvertimeRequest",
            entity.Id,
            cancellationToken);

        if (employee.SupervisorId.HasValue)
        {
            var supervisor = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == employee.SupervisorId, cancellationToken);
            if (!string.IsNullOrEmpty(supervisor?.UserId))
            {
                await _notifications.NotifyUserAsync(
                    supervisor.UserId,
                    "Overtime submitted",
                    $"{employee.FirstName} {employee.LastName} submitted overtime for {request.OvertimeDate:yyyy-MM-dd}.",
                    NotificationType.OvertimeSubmitted,
                    entity.Id.ToString(),
                    "OvertimeRequest",
                    cancellationToken);
            }
        }

        return ApiResponse<OvertimeRequestDto>.Ok(await MapAsync(entity.Id, cancellationToken)!, "Overtime submitted.");
    }

    public async Task<ApiResponse<PagedResult<OvertimeRequestDto>>> GetAsync(
        OvertimeListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q =
            from o in _db.OvertimeRequests.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on o.EmployeeId equals e.Id
            select new { o, e };

        if (query.EmployeeId.HasValue) q = q.Where(x => x.o.EmployeeId == query.EmployeeId);
        if (query.Status.HasValue) q = q.Where(x => x.o.Status == query.Status);
        if (query.From.HasValue) q = q.Where(x => x.o.OvertimeDate >= query.From);
        if (query.To.HasValue) q = q.Where(x => x.o.OvertimeDate <= query.To);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(x => x.o.CreatedAtUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new OvertimeRequestDto(
                x.o.Id, x.o.EmployeeId, x.e.EmployeeCode, x.e.FirstName + " " + x.e.LastName,
                x.o.OvertimeDate, x.o.RequestedHours, x.o.ApprovedHours, x.o.Reason, x.o.Remarks,
                x.o.Status, x.o.IsPayable, x.o.RequestedByUserId, x.o.ApprovedByUserId,
                x.o.ApprovedAtUtc, x.o.ApproverRemarks, x.o.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<OvertimeRequestDto>>.Ok(
            PagedResult<OvertimeRequestDto>.Create(items, total, query.PageNumber, query.PageSize));
    }

    public async Task<ApiResponse<OvertimeRequestDto>> ApproveAsync(
        string approverUserId,
        Guid id,
        ApproveOvertimeRequest request,
        CancellationToken cancellationToken = default)
    {
        OvertimeRequest? entity = null;
        try
        {
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                entity = await _db.OvertimeRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (entity is null)
                    throw new InvalidOperationException("Overtime request not found.");
                if (entity.Status != AttendanceApprovalStatus.Pending)
                    throw new InvalidOperationException("Only pending overtime requests can be approved.");

                if (await _lockService.IsMonthLockedAsync(entity.OvertimeDate.Year, entity.OvertimeDate.Month, ct))
                    throw new InvalidOperationException("Attendance is locked for this month.");

                var approvedHours = request.ApprovedHours ?? entity.RequestedHours;
                if (approvedHours <= 0)
                    throw new InvalidOperationException("Approved hours must be greater than zero.");

                entity.Status = AttendanceApprovalStatus.Approved;
                entity.ApprovedHours = approvedHours;
                entity.IsPayable = request.IsPayable;
                entity.ApprovedByUserId = approverUserId;
                entity.ApprovedAtUtc = DateTime.UtcNow;
                entity.ApproverRemarks = request.Remarks;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                _db.Update(entity);

                var day = await _db.EmployeeAttendanceDays
                    .FirstOrDefaultAsync(x => x.EmployeeId == entity.EmployeeId && x.AttendanceDate == entity.OvertimeDate, ct);
                if (day is not null)
                {
                    day.OvertimeMinutes = (int)Math.Round(approvedHours * 60m);
                    day.UpdatedAtUtc = DateTime.UtcNow;
                    _db.Update(day);
                }

                await _db.SaveChangesAsync(ct);

                await _audit.LogAsync(
                    AttendanceAuditAction.OvertimeApproved,
                    approverUserId,
                    entity.EmployeeId,
                    entity.OvertimeDate,
                    $"{entity.RequestedHours}h",
                    $"{approvedHours}h",
                    request.Remarks,
                    null,
                    "OvertimeRequest",
                    entity.Id,
                    ct);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<OvertimeRequestDto>.Fail(ex.Message);
        }

        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity!.EmployeeId, cancellationToken);
        if (!string.IsNullOrEmpty(employee?.UserId))
        {
            await _notifications.NotifyUserAsync(
                employee.UserId,
                "Overtime approved",
                $"Your overtime for {entity!.OvertimeDate:yyyy-MM-dd} was approved.",
                NotificationType.OvertimeApproved,
                entity.Id.ToString(),
                "OvertimeRequest",
                cancellationToken);
        }

        return ApiResponse<OvertimeRequestDto>.Ok(await MapAsync(id, cancellationToken)!, "Overtime approved.");
    }

    public async Task<ApiResponse<OvertimeRequestDto>> RejectAsync(
        string approverUserId,
        Guid id,
        ApprovalActionRequest request,
        CancellationToken cancellationToken = default)
    {
        OvertimeRequest? entity = null;
        try
        {
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                entity = await _db.OvertimeRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (entity is null)
                    throw new InvalidOperationException("Overtime request not found.");
                if (entity.Status != AttendanceApprovalStatus.Pending)
                    throw new InvalidOperationException("Only pending overtime requests can be rejected.");

                entity.Status = AttendanceApprovalStatus.Rejected;
                entity.RejectedByUserId = approverUserId;
                entity.RejectedAtUtc = DateTime.UtcNow;
                entity.ApproverRemarks = request.Remarks;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                _db.Update(entity);
                await _db.SaveChangesAsync(ct);

                await _audit.LogAsync(
                    AttendanceAuditAction.OvertimeRejected,
                    approverUserId,
                    entity.EmployeeId,
                    entity.OvertimeDate,
                    null,
                    AttendanceApprovalStatus.Rejected.ToString(),
                    request.Remarks,
                    null,
                    "OvertimeRequest",
                    entity.Id,
                    ct);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<OvertimeRequestDto>.Fail(ex.Message);
        }

        return ApiResponse<OvertimeRequestDto>.Ok(await MapAsync(id, cancellationToken)!, "Overtime rejected.");
    }

    private async Task<OvertimeRequestDto?> MapAsync(Guid id, CancellationToken cancellationToken)
    {
        return await (
            from o in _db.OvertimeRequests.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on o.EmployeeId equals e.Id
            where o.Id == id
            select new OvertimeRequestDto(
                o.Id, o.EmployeeId, e.EmployeeCode, e.FirstName + " " + e.LastName,
                o.OvertimeDate, o.RequestedHours, o.ApprovedHours, o.Reason, o.Remarks,
                o.Status, o.IsPayable, o.RequestedByUserId, o.ApprovedByUserId,
                o.ApprovedAtUtc, o.ApproverRemarks, o.CreatedAtUtc)
        ).FirstOrDefaultAsync(cancellationToken);
    }
}
