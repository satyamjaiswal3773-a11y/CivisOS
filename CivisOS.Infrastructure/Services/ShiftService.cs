using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class AttendanceAuditService : IAttendanceAuditService
{
    private readonly IApplicationDbContext _db;

    public AttendanceAuditService(IApplicationDbContext db) => _db = db;

    public async Task LogAsync(
        AttendanceAuditAction action,
        string? userId,
        Guid? employeeId,
        DateOnly? attendanceDate,
        string? oldValue,
        string? newValue,
        string? reason,
        string? ipAddress,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default)
    {
        _db.Add(new AttendanceAuditLog
        {
            Action = action,
            UserId = userId,
            EmployeeId = employeeId,
            AttendanceDate = attendanceDate,
            OldValue = Truncate(oldValue, 4000),
            NewValue = Truncate(newValue, 4000),
            Reason = Truncate(reason, 1000),
            IpAddress = Truncate(ipAddress, 64),
            RelatedEntityType = Truncate(relatedEntityType, 100),
            RelatedEntityId = relatedEntityId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ApiResponse<PagedResult<AuditLogDto>>> GetAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.AttendanceAuditLogs.AsNoTracking().AsQueryable();
        if (query.EmployeeId.HasValue) q = q.Where(x => x.EmployeeId == query.EmployeeId);
        if (query.Action.HasValue) q = q.Where(x => x.Action == query.Action);
        if (query.From.HasValue)
        {
            var fromUtc = query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            q = q.Where(x => x.CreatedAtUtc >= fromUtc);
        }
        if (query.To.HasValue)
        {
            var toUtc = query.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            q = q.Where(x => x.CreatedAtUtc <= toUtc);
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AuditLogDto(
                x.Id, x.UserId, x.EmployeeId, x.AttendanceDate, x.Action,
                x.OldValue, x.NewValue, x.Reason, x.IpAddress, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<AuditLogDto>>.Ok(PagedResult<AuditLogDto>.Create(items, total, query.PageNumber, query.PageSize));
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}

public class ShiftService : IShiftService
{
    private readonly IApplicationDbContext _db;
    private readonly IAttendanceAuditService _audit;

    public ShiftService(IApplicationDbContext db, IAttendanceAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<ApiResponse<PagedResult<ShiftDto>>> GetShiftsAsync(ShiftListQuery query, CancellationToken cancellationToken = default)
    {
        var q = _db.ShiftMasters.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(x => x.ShiftCode.Contains(s) || x.ShiftName.Contains(s));
        }
        if (query.IsActive.HasValue) q = q.Where(x => x.IsActive == query.IsActive);

        var total = await q.CountAsync(cancellationToken);
        var entities = await q.OrderBy(x => x.ShiftName)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var items = entities.Select(MapShift).ToList();

        return ApiResponse<PagedResult<ShiftDto>>.Ok(PagedResult<ShiftDto>.Create(items, total, query.PageNumber, query.PageSize));
    }

    public async Task<ApiResponse<ShiftDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shift = await _db.ShiftMasters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return shift is null
            ? ApiResponse<ShiftDto>.Fail("Shift not found.")
            : ApiResponse<ShiftDto>.Ok(MapShift(shift));
    }

    public async Task<ApiResponse<ShiftDto>> CreateAsync(CreateShiftRequest request, CancellationToken cancellationToken = default)
    {
        if (await _db.ShiftMasters.AnyAsync(x => x.ShiftCode == request.ShiftCode, cancellationToken))
            return ApiResponse<ShiftDto>.Fail("Shift code already exists.");

        var entity = new ShiftMaster
        {
            ShiftCode = request.ShiftCode.Trim(),
            ShiftName = request.ShiftName.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            GracePeriodMinutes = request.GracePeriodMinutes,
            MinimumWorkingMinutes = request.MinimumWorkingMinutes,
            AllowedBreakMinutes = request.AllowedBreakMinutes,
            LateAfterMinutes = request.LateAfterMinutes,
            EarlyLeavingAfterMinutes = request.EarlyLeavingAfterMinutes,
            OvertimeAllowed = request.OvertimeAllowed,
            OvertimeAfterMinutes = request.OvertimeAfterMinutes,
            IsNightShift = request.IsNightShift,
            IsCrossMidnight = request.IsCrossMidnight || request.EndTime <= request.StartTime,
            Description = request.Description
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<ShiftDto>.Ok(MapShift(entity), "Shift created.");
    }

    public async Task<ApiResponse<ShiftDto>> UpdateAsync(Guid id, UpdateShiftRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ShiftMasters.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return ApiResponse<ShiftDto>.Fail("Shift not found.");

        entity.ShiftName = request.ShiftName.Trim();
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.GracePeriodMinutes = request.GracePeriodMinutes;
        entity.MinimumWorkingMinutes = request.MinimumWorkingMinutes;
        entity.AllowedBreakMinutes = request.AllowedBreakMinutes;
        entity.LateAfterMinutes = request.LateAfterMinutes;
        entity.EarlyLeavingAfterMinutes = request.EarlyLeavingAfterMinutes;
        entity.OvertimeAllowed = request.OvertimeAllowed;
        entity.OvertimeAfterMinutes = request.OvertimeAfterMinutes;
        entity.IsNightShift = request.IsNightShift;
        entity.IsCrossMidnight = request.IsCrossMidnight || request.EndTime <= request.StartTime;
        entity.IsActive = request.IsActive;
        entity.Description = request.Description;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _db.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<ShiftDto>.Ok(MapShift(entity), "Shift updated.");
    }

    public async Task<ApiResponse> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ShiftMasters.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return ApiResponse.Fail("Shift not found.");
        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Shift deactivated.");
    }

    public async Task<ApiResponse<EmployeeShiftDto>> AssignAsync(string actorUserId, AssignShiftRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.EmployeeId, cancellationToken);
        if (employee is null) return ApiResponse<EmployeeShiftDto>.Fail("Employee not found.");

        var shift = await _db.ShiftMasters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ShiftId && x.IsActive, cancellationToken);
        if (shift is null) return ApiResponse<EmployeeShiftDto>.Fail("Active shift not found.");

        if (request.EffectiveTo.HasValue && request.EffectiveTo < request.EffectiveFrom)
            return ApiResponse<EmployeeShiftDto>.Fail("EffectiveTo cannot be earlier than EffectiveFrom.");

        var active = await _db.EmployeeShiftAssignments
            .Where(x => x.EmployeeId == request.EmployeeId && x.IsActive &&
                        (x.EffectiveTo == null || x.EffectiveTo >= request.EffectiveFrom))
            .ToListAsync(cancellationToken);

        foreach (var a in active)
        {
            a.EffectiveTo = request.EffectiveFrom.AddDays(-1);
            a.IsActive = a.EffectiveTo >= a.EffectiveFrom;
            a.UpdatedAtUtc = DateTime.UtcNow;
            _db.Update(a);
        }

        var assignment = new EmployeeShiftAssignment
        {
            EmployeeId = request.EmployeeId,
            ShiftId = request.ShiftId,
            DepartmentId = request.DepartmentId ?? employee.DepartmentId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Remarks = request.Remarks,
            AssignedByUserId = actorUserId,
            IsActive = true
        };
        _db.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(AttendanceAuditAction.ShiftAssigned, actorUserId, request.EmployeeId, request.EffectiveFrom,
            null, $"ShiftId={request.ShiftId}", request.Remarks, null, "EmployeeShiftAssignment", assignment.Id, cancellationToken);

        return ApiResponse<EmployeeShiftDto>.Ok(await MapAssignmentAsync(assignment.Id, cancellationToken)!, "Shift assigned.");
    }

    public async Task<ApiResponse<int>> BulkAssignAsync(string actorUserId, BulkAssignShiftRequest request, CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var employeeId in request.EmployeeIds.Distinct())
        {
            var result = await AssignAsync(actorUserId, new AssignShiftRequest(
                employeeId, request.ShiftId, request.EffectiveFrom, request.EffectiveTo, null, request.Remarks), cancellationToken);
            if (result.Success) count++;
        }
        return ApiResponse<int>.Ok(count, $"Assigned shift to {count} employee(s).");
    }

    public async Task<ApiResponse<EmployeeShiftDto>> GetEmployeeShiftAsync(Guid employeeId, DateOnly? asOf = null, CancellationToken cancellationToken = default)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var assignment = await _db.EmployeeShiftAssignments.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null) return ApiResponse<EmployeeShiftDto>.Fail("No shift assignment found for the employee.");
        var dto = await MapAssignmentAsync(assignment.Id, cancellationToken);
        return ApiResponse<EmployeeShiftDto>.Ok(dto!);
    }

    public async Task<ApiResponse<PagedResult<EmployeeShiftDto>>> GetShiftHistoryAsync(Guid employeeId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var q = _db.EmployeeShiftAssignments.AsNoTracking().Where(x => x.EmployeeId == employeeId);
        var total = await q.CountAsync(cancellationToken);
        var ids = await q.OrderByDescending(x => x.EffectiveFrom)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var items = new List<EmployeeShiftDto>();
        foreach (var id in ids)
        {
            var dto = await MapAssignmentAsync(id, cancellationToken);
            if (dto is not null) items.Add(dto);
        }

        return ApiResponse<PagedResult<EmployeeShiftDto>>.Ok(PagedResult<EmployeeShiftDto>.Create(items, total, pageNumber, pageSize));
    }

    private async Task<EmployeeShiftDto?> MapAssignmentAsync(Guid id, CancellationToken cancellationToken)
    {
        return await (
            from a in _db.EmployeeShiftAssignments.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on a.EmployeeId equals e.Id
            join s in _db.ShiftMasters.AsNoTracking() on a.ShiftId equals s.Id
            where a.Id == id
            select new EmployeeShiftDto(
                a.Id, a.EmployeeId, e.EmployeeCode, e.FirstName + " " + e.LastName,
                a.ShiftId, s.ShiftName, a.EffectiveFrom, a.EffectiveTo, a.IsActive, a.Remarks)
        ).FirstOrDefaultAsync(cancellationToken);
    }

    private static ShiftDto MapShift(ShiftMaster x) => new(
        x.Id, x.ShiftCode, x.ShiftName, x.StartTime, x.EndTime,
        x.GracePeriodMinutes, x.MinimumWorkingMinutes, x.AllowedBreakMinutes,
        x.LateAfterMinutes, x.EarlyLeavingAfterMinutes, x.OvertimeAllowed, x.OvertimeAfterMinutes,
        x.IsNightShift, x.IsCrossMidnight, x.IsActive, x.Description, x.CreatedAtUtc);
}
