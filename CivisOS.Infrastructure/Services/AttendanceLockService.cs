using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Notifications.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class AttendanceLockService : IAttendanceLockService
{
    private readonly IApplicationDbContext _db;
    private readonly IAttendanceAuditService _audit;
    private readonly INotificationService _notifications;

    public AttendanceLockService(
        IApplicationDbContext db,
        IAttendanceAuditService audit,
        INotificationService notifications)
    {
        _db = db;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task<bool> IsMonthLockedAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var status = await _db.AttendanceLocks.AsNoTracking()
            .Where(x => x.Year == year && x.Month == month)
            .Select(x => (AttendanceLockStatus?)x.Status)
            .FirstOrDefaultAsync(cancellationToken);

        return status is AttendanceLockStatus.Locked or AttendanceLockStatus.Finalized;
    }

    public async Task<ApiResponse<AttendanceLockDto>> FinalizeMonthAsync(
        string userId,
        MonthActionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Month is < 1 or > 12)
            return ApiResponse<AttendanceLockDto>.Fail("Invalid month.");

        AttendanceLock? lockEntity = null;

        try
        {
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                lockEntity = await _db.AttendanceLocks
                    .FirstOrDefaultAsync(x => x.Year == request.Year && x.Month == request.Month, ct);

                if (lockEntity is null)
                {
                    lockEntity = new AttendanceLock
                    {
                        Year = request.Year,
                        Month = request.Month
                    };
                    _db.Add(lockEntity);
                }

                if (lockEntity.Status == AttendanceLockStatus.Locked)
                    throw new InvalidOperationException("Month is already locked.");

                lockEntity.Status = AttendanceLockStatus.Finalized;
                lockEntity.FinalizedByUserId = userId;
                lockEntity.FinalizedAtUtc = DateTime.UtcNow;
                lockEntity.Remarks = request.Remarks;
                lockEntity.UpdatedAtUtc = DateTime.UtcNow;
                _db.Update(lockEntity);

                var from = new DateOnly(request.Year, request.Month, 1);
                var to = from.AddMonths(1).AddDays(-1);
                var days = await _db.EmployeeAttendanceDays
                    .Where(x => x.AttendanceDate >= from && x.AttendanceDate <= to)
                    .ToListAsync(ct);

                foreach (var day in days)
                {
                    day.IsFinalized = true;
                    day.UpdatedAtUtc = DateTime.UtcNow;
                    _db.Update(day);
                }

                await _db.SaveChangesAsync(ct);

                await _audit.LogAsync(
                    AttendanceAuditAction.MonthFinalized,
                    userId,
                    null,
                    from,
                    null,
                    $"Year={request.Year},Month={request.Month}",
                    request.Remarks,
                    null,
                    "AttendanceLock",
                    lockEntity.Id,
                    ct);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<AttendanceLockDto>.Fail(ex.Message);
        }

        var employees = await _db.Employees.AsNoTracking()
            .Where(x => x.IsActive && x.UserId != null)
            .Select(x => x.UserId!)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var uid in employees)
        {
            await _notifications.NotifyUserAsync(
                uid,
                "Attendance finalized",
                $"Attendance for {request.Year}-{request.Month:D2} has been finalized.",
                NotificationType.AttendanceFinalized,
                lockEntity!.Id.ToString(),
                "AttendanceLock",
                cancellationToken);
        }

        return ApiResponse<AttendanceLockDto>.Ok(Map(lockEntity!), "Month finalized.");
    }

    public async Task<ApiResponse<AttendanceLockDto>> LockMonthAsync(
        string userId,
        MonthActionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Month is < 1 or > 12)
            return ApiResponse<AttendanceLockDto>.Fail("Invalid month.");

        AttendanceLock? lockEntity = null;

        try
        {
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                lockEntity = await _db.AttendanceLocks
                    .FirstOrDefaultAsync(x => x.Year == request.Year && x.Month == request.Month, ct);

                if (lockEntity is null)
                {
                    lockEntity = new AttendanceLock
                    {
                        Year = request.Year,
                        Month = request.Month
                    };
                    _db.Add(lockEntity);
                }

                if (lockEntity.Status == AttendanceLockStatus.Locked)
                    throw new InvalidOperationException("Month is already locked.");

                lockEntity.Status = AttendanceLockStatus.Locked;
                lockEntity.LockedByUserId = userId;
                lockEntity.LockedAtUtc = DateTime.UtcNow;
                lockEntity.Remarks = request.Remarks;
                lockEntity.UpdatedAtUtc = DateTime.UtcNow;
                _db.Update(lockEntity);
                await _db.SaveChangesAsync(ct);

                await _audit.LogAsync(
                    AttendanceAuditAction.MonthLocked,
                    userId,
                    null,
                    new DateOnly(request.Year, request.Month, 1),
                    null,
                    $"Year={request.Year},Month={request.Month}",
                    request.Remarks,
                    null,
                    "AttendanceLock",
                    lockEntity.Id,
                    ct);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<AttendanceLockDto>.Fail(ex.Message);
        }

        return ApiResponse<AttendanceLockDto>.Ok(Map(lockEntity!), "Month locked.");
    }

    public async Task<ApiResponse<AttendanceLockDto>> UnlockMonthAsync(
        string userId,
        UnlockMonthRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return ApiResponse<AttendanceLockDto>.Fail("Unlock reason is required.");

        AttendanceLock? lockEntity = null;

        try
        {
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                lockEntity = await _db.AttendanceLocks
                    .FirstOrDefaultAsync(x => x.Year == request.Year && x.Month == request.Month, ct);

                if (lockEntity is null)
                    throw new InvalidOperationException("Attendance lock record not found.");

                if (lockEntity.Status == AttendanceLockStatus.Open)
                    throw new InvalidOperationException("Month is already open.");

                var oldStatus = lockEntity.Status.ToString();
                lockEntity.Status = AttendanceLockStatus.Open;
                lockEntity.UnlockedByUserId = userId;
                lockEntity.UnlockedAtUtc = DateTime.UtcNow;
                lockEntity.UnlockReason = request.Reason.Trim();
                lockEntity.UpdatedAtUtc = DateTime.UtcNow;
                _db.Update(lockEntity);

                var from = new DateOnly(request.Year, request.Month, 1);
                var to = from.AddMonths(1).AddDays(-1);
                var days = await _db.EmployeeAttendanceDays
                    .Where(x => x.AttendanceDate >= from && x.AttendanceDate <= to && x.IsFinalized)
                    .ToListAsync(ct);

                foreach (var day in days)
                {
                    day.IsFinalized = false;
                    day.UpdatedAtUtc = DateTime.UtcNow;
                    _db.Update(day);
                }

                await _db.SaveChangesAsync(ct);

                await _audit.LogAsync(
                    AttendanceAuditAction.MonthUnlocked,
                    userId,
                    null,
                    from,
                    oldStatus,
                    AttendanceLockStatus.Open.ToString(),
                    request.Reason,
                    ipAddress,
                    "AttendanceLock",
                    lockEntity.Id,
                    ct);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<AttendanceLockDto>.Fail(ex.Message);
        }

        return ApiResponse<AttendanceLockDto>.Ok(Map(lockEntity!), "Month unlocked.");
    }

    public async Task<ApiResponse<IReadOnlyList<AttendanceLockDto>>> GetLocksAsync(
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var q = _db.AttendanceLocks.AsNoTracking().AsQueryable();
        if (year.HasValue) q = q.Where(x => x.Year == year);

        var items = await q.OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .Select(x => new AttendanceLockDto(
                x.Id, x.Year, x.Month, x.Status,
                x.FinalizedByUserId, x.FinalizedAtUtc,
                x.LockedByUserId, x.LockedAtUtc,
                x.UnlockedByUserId, x.UnlockedAtUtc,
                x.UnlockReason, x.Remarks))
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyList<AttendanceLockDto>>.Ok(items);
    }

    private static AttendanceLockDto Map(AttendanceLock x) => new(
        x.Id, x.Year, x.Month, x.Status,
        x.FinalizedByUserId, x.FinalizedAtUtc,
        x.LockedByUserId, x.LockedAtUtc,
        x.UnlockedByUserId, x.UnlockedAtUtc,
        x.UnlockReason, x.Remarks);
}
