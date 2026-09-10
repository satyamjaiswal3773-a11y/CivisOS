using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Notifications.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class AttendanceRegularizationService : IAttendanceRegularizationService
{
    private readonly IApplicationDbContext _db;
    private readonly IAttendanceLockService _lockService;
    private readonly IAttendanceAuditService _audit;
    private readonly INotificationService _notifications;

    public AttendanceRegularizationService(
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

    public async Task<ApiResponse<RegularizationDto>> SubmitAsync(
        string userId,
        CreateRegularizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var employeeId = request.EmployeeId;
        if (!employeeId.HasValue)
        {
            var self = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
            if (self is null) return ApiResponse<RegularizationDto>.Fail("Employee profile not found.");
            employeeId = self.Id;
        }

        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == employeeId && x.IsActive, cancellationToken);
        if (employee is null) return ApiResponse<RegularizationDto>.Fail("Employee not found or inactive.");

        if (await _lockService.IsMonthLockedAsync(request.AttendanceDate.Year, request.AttendanceDate.Month, cancellationToken))
            return ApiResponse<RegularizationDto>.Fail("Attendance is locked for this month.");

        if (string.IsNullOrWhiteSpace(request.Reason))
            return ApiResponse<RegularizationDto>.Fail("Reason is required.");

        var pendingExists = await _db.AttendanceRegularizations.AsNoTracking().AnyAsync(x =>
            x.EmployeeId == employeeId
            && x.AttendanceDate == request.AttendanceDate
            && x.Status == AttendanceApprovalStatus.Pending, cancellationToken);
        if (pendingExists)
            return ApiResponse<RegularizationDto>.Fail("A pending regularization already exists for this date.");

        var entity = new AttendanceRegularization
        {
            EmployeeId = employeeId.Value,
            AttendanceDate = request.AttendanceDate,
            RequestedInUtc = request.RequestedInUtc,
            RequestedOutUtc = request.RequestedOutUtc,
            Reason = request.Reason.Trim(),
            Remarks = request.Remarks,
            AttachmentReference = request.AttachmentReference,
            RequestedByUserId = userId,
            Status = AttendanceApprovalStatus.Pending
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AttendanceAuditAction.RegularizationSubmitted,
            userId,
            employeeId,
            request.AttendanceDate,
            null,
            $"In={request.RequestedInUtc:u},Out={request.RequestedOutUtc:u}",
            request.Reason,
            null,
            "AttendanceRegularization",
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
                    "Regularization submitted",
                    $"{employee.FirstName} {employee.LastName} submitted regularization for {request.AttendanceDate:yyyy-MM-dd}.",
                    NotificationType.RegularizationSubmitted,
                    entity.Id.ToString(),
                    "AttendanceRegularization",
                    cancellationToken);
            }
        }

        return ApiResponse<RegularizationDto>.Ok(await MapAsync(entity.Id, cancellationToken)!, "Regularization submitted.");
    }

    public async Task<ApiResponse<PagedResult<RegularizationDto>>> GetAsync(
        RegularizationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q =
            from r in _db.AttendanceRegularizations.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on r.EmployeeId equals e.Id
            select new { r, e };

        if (query.EmployeeId.HasValue) q = q.Where(x => x.r.EmployeeId == query.EmployeeId);
        if (query.Status.HasValue) q = q.Where(x => x.r.Status == query.Status);
        if (query.From.HasValue) q = q.Where(x => x.r.AttendanceDate >= query.From);
        if (query.To.HasValue) q = q.Where(x => x.r.AttendanceDate <= query.To);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(x => x.r.CreatedAtUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new RegularizationDto(
                x.r.Id, x.r.EmployeeId, x.e.EmployeeCode, x.e.FirstName + " " + x.e.LastName,
                x.r.AttendanceDate, x.r.RequestedInUtc, x.r.RequestedOutUtc, x.r.Reason, x.r.Remarks, x.r.AttachmentReference,
                x.r.Status, x.r.RequestedByUserId, x.r.ApprovedByUserId, x.r.ApprovedAtUtc,
                x.r.RejectedByUserId, x.r.RejectedAtUtc, x.r.ApproverRemarks, x.r.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<RegularizationDto>>.Ok(
            PagedResult<RegularizationDto>.Create(items, total, query.PageNumber, query.PageSize));
    }

    public async Task<ApiResponse<RegularizationDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await MapAsync(id, cancellationToken);
        return dto is null
            ? ApiResponse<RegularizationDto>.Fail("Regularization not found.")
            : ApiResponse<RegularizationDto>.Ok(dto);
    }

    public async Task<ApiResponse<RegularizationDto>> ApproveAsync(
        string approverUserId,
        Guid id,
        ApprovalActionRequest request,
        CancellationToken cancellationToken = default)
    {
        AttendanceRegularization? entity = null;

        try
        {
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                entity = await _db.AttendanceRegularizations.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (entity is null)
                    throw new InvalidOperationException("Regularization not found.");
                if (entity.Status is not (AttendanceApprovalStatus.Pending or AttendanceApprovalStatus.SentBack))
                    throw new InvalidOperationException("Regularization cannot be approved in its current status.");

                if (await _lockService.IsMonthLockedAsync(entity.AttendanceDate.Year, entity.AttendanceDate.Month, ct))
                    throw new InvalidOperationException("Attendance is locked for this month.");

                entity.Status = AttendanceApprovalStatus.Approved;
                entity.ApprovedByUserId = approverUserId;
                entity.ApprovedAtUtc = DateTime.UtcNow;
                entity.ApproverRemarks = request.Remarks;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                _db.Update(entity);

                var day = await _db.EmployeeAttendanceDays
                    .FirstOrDefaultAsync(x => x.EmployeeId == entity.EmployeeId && x.AttendanceDate == entity.AttendanceDate, ct);

                if (day is null)
                {
                    day = new EmployeeAttendanceDay
                    {
                        EmployeeId = entity.EmployeeId,
                        AttendanceDate = entity.AttendanceDate
                    };
                    _db.Add(day);
                }
                else
                {
                    day.UpdatedAtUtc = DateTime.UtcNow;
                }

                // Do not modify raw punches — apply override on processed day
                if (entity.RequestedInUtc.HasValue) day.FirstInUtc = entity.RequestedInUtc;
                if (entity.RequestedOutUtc.HasValue) day.LastOutUtc = entity.RequestedOutUtc;
                day.IsManualOverride = true;
                day.HasMissingPunch = false;
                day.Status = DayAttendanceStatus.Present;
                day.AttendanceSource = PunchSource.Admin;
                day.Remarks = string.IsNullOrWhiteSpace(request.Remarks)
                    ? $"Regularization approved: {entity.Reason}"
                    : request.Remarks;

                if (day.FirstInUtc.HasValue && day.LastOutUtc.HasValue && day.LastOutUtc > day.FirstInUtc)
                    day.WorkingMinutes = (int)Math.Round((day.LastOutUtc.Value - day.FirstInUtc.Value).TotalMinutes);

                if (day.Id != Guid.Empty)
                    _db.Update(day);

                await _db.SaveChangesAsync(ct);

                await _audit.LogAsync(
                    AttendanceAuditAction.RegularizationApproved,
                    approverUserId,
                    entity.EmployeeId,
                    entity.AttendanceDate,
                    null,
                    $"In={entity.RequestedInUtc:u},Out={entity.RequestedOutUtc:u}",
                    request.Remarks ?? entity.Reason,
                    null,
                    "AttendanceRegularization",
                    entity.Id,
                    ct);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<RegularizationDto>.Fail(ex.Message);
        }

        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity!.EmployeeId, cancellationToken);
        if (!string.IsNullOrEmpty(employee?.UserId))
        {
            await _notifications.NotifyUserAsync(
                employee.UserId,
                "Regularization approved",
                $"Your regularization for {entity!.AttendanceDate:yyyy-MM-dd} was approved.",
                NotificationType.RegularizationApproved,
                entity.Id.ToString(),
                "AttendanceRegularization",
                cancellationToken);
        }

        return ApiResponse<RegularizationDto>.Ok(await MapAsync(id, cancellationToken)!, "Regularization approved.");
    }

    public async Task<ApiResponse<RegularizationDto>> RejectAsync(
        string approverUserId,
        Guid id,
        ApprovalActionRequest request,
        CancellationToken cancellationToken = default)
    {
        AttendanceRegularization? entity = null;
        try
        {
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                entity = await _db.AttendanceRegularizations.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (entity is null)
                    throw new InvalidOperationException("Regularization not found.");
                if (entity.Status is not (AttendanceApprovalStatus.Pending or AttendanceApprovalStatus.SentBack))
                    throw new InvalidOperationException("Regularization cannot be rejected in its current status.");

                entity.Status = AttendanceApprovalStatus.Rejected;
                entity.RejectedByUserId = approverUserId;
                entity.RejectedAtUtc = DateTime.UtcNow;
                entity.ApproverRemarks = request.Remarks;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                _db.Update(entity);
                await _db.SaveChangesAsync(ct);

                await _audit.LogAsync(
                    AttendanceAuditAction.RegularizationRejected,
                    approverUserId,
                    entity.EmployeeId,
                    entity.AttendanceDate,
                    null,
                    AttendanceApprovalStatus.Rejected.ToString(),
                    request.Remarks,
                    null,
                    "AttendanceRegularization",
                    entity.Id,
                    ct);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<RegularizationDto>.Fail(ex.Message);
        }

        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity!.EmployeeId, cancellationToken);
        if (!string.IsNullOrEmpty(employee?.UserId))
        {
            await _notifications.NotifyUserAsync(
                employee.UserId,
                "Regularization rejected",
                $"Your regularization for {entity!.AttendanceDate:yyyy-MM-dd} was rejected.",
                NotificationType.RegularizationRejected,
                entity.Id.ToString(),
                "AttendanceRegularization",
                cancellationToken);
        }

        return ApiResponse<RegularizationDto>.Ok(await MapAsync(id, cancellationToken)!, "Regularization rejected.");
    }

    public async Task<ApiResponse<RegularizationDto>> SendBackAsync(
        string approverUserId,
        Guid id,
        ApprovalActionRequest request,
        CancellationToken cancellationToken = default)
    {
        AttendanceRegularization? entity = null;
        try
        {
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                entity = await _db.AttendanceRegularizations.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (entity is null)
                    throw new InvalidOperationException("Regularization not found.");
                if (entity.Status != AttendanceApprovalStatus.Pending)
                    throw new InvalidOperationException("Only pending regularizations can be sent back.");

                entity.Status = AttendanceApprovalStatus.SentBack;
                entity.ApproverRemarks = request.Remarks;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                _db.Update(entity);
                await _db.SaveChangesAsync(ct);

                await _audit.LogAsync(
                    AttendanceAuditAction.RegularizationSentBack,
                    approverUserId,
                    entity.EmployeeId,
                    entity.AttendanceDate,
                    null,
                    AttendanceApprovalStatus.SentBack.ToString(),
                    request.Remarks,
                    null,
                    "AttendanceRegularization",
                    entity.Id,
                    ct);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<RegularizationDto>.Fail(ex.Message);
        }

        return ApiResponse<RegularizationDto>.Ok(await MapAsync(id, cancellationToken)!, "Regularization sent back.");
    }

    private async Task<RegularizationDto?> MapAsync(Guid id, CancellationToken cancellationToken)
    {
        return await (
            from r in _db.AttendanceRegularizations.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on r.EmployeeId equals e.Id
            where r.Id == id
            select new RegularizationDto(
                r.Id, r.EmployeeId, e.EmployeeCode, e.FirstName + " " + e.LastName,
                r.AttendanceDate, r.RequestedInUtc, r.RequestedOutUtc, r.Reason, r.Remarks, r.AttachmentReference,
                r.Status, r.RequestedByUserId, r.ApprovedByUserId, r.ApprovedAtUtc,
                r.RejectedByUserId, r.RejectedAtUtc, r.ApproverRemarks, r.CreatedAtUtc)
        ).FirstOrDefaultAsync(cancellationToken);
    }
}
