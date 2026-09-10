using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Notifications.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class AttendanceManagementService : IAttendanceManagementService
{
    private readonly IApplicationDbContext _db;
    private readonly IAttendanceLockService _lockService;
    private readonly IAttendanceAuditService _audit;
    private readonly INotificationService _notifications;

    public AttendanceManagementService(
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

    public async Task<ApiResponse<PagedResult<EmployeeAttendanceDayDto>>> GetDailyAsync(
        DailyAttendanceQuery query,
        CancellationToken cancellationToken = default)
    {
        var date = query.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var q =
            from d in _db.EmployeeAttendanceDays.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on d.EmployeeId equals e.Id
            join dep in _db.Departments.AsNoTracking() on e.DepartmentId equals dep.Id into deps
            from dep in deps.DefaultIfEmpty()
            join des in _db.Designations.AsNoTracking() on e.DesignationId equals des.Id into dess
            from des in dess.DefaultIfEmpty()
            join s in _db.ShiftMasters.AsNoTracking() on d.ShiftId equals s.Id into shifts
            from s in shifts.DefaultIfEmpty()
            where d.AttendanceDate == date
            select new { d, e, dep, des, s };

        if (query.EmployeeId.HasValue) q = q.Where(x => x.d.EmployeeId == query.EmployeeId);
        if (query.DepartmentId.HasValue) q = q.Where(x => x.e.DepartmentId == query.DepartmentId);
        if (query.DesignationId.HasValue) q = q.Where(x => x.e.DesignationId == query.DesignationId);
        if (query.ShiftId.HasValue) q = q.Where(x => x.d.ShiftId == query.ShiftId);
        if (query.Status.HasValue) q = q.Where(x => x.d.Status == query.Status);
        if (query.Source.HasValue) q = q.Where(x => x.d.AttendanceSource == query.Source);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderBy(x => x.e.EmployeeCode)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new EmployeeAttendanceDayDto(
                x.d.Id, x.d.EmployeeId, x.e.EmployeeCode, x.e.FirstName + " " + x.e.LastName,
                x.e.DepartmentId, x.dep != null ? x.dep.Name : null,
                x.e.DesignationId, x.des != null ? x.des.Name : null,
                x.d.AttendanceDate, x.d.ShiftId, x.s != null ? x.s.ShiftName : null,
                x.d.FirstInUtc, x.d.LastOutUtc, x.d.TotalPunches, x.d.BreakMinutes, x.d.WorkingMinutes,
                x.d.LateMinutes, x.d.EarlyOutMinutes, x.d.OvertimeMinutes, x.d.ExcessBreakMinutes,
                x.d.Status, x.d.AttendanceSource, x.d.IsLate, x.d.IsEarlyLeaving, x.d.HasMissingPunch,
                x.d.IsFinalized, x.d.IsManualOverride, x.d.Remarks))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<EmployeeAttendanceDayDto>>.Ok(
            PagedResult<EmployeeAttendanceDayDto>.Create(items, total, query.PageNumber, query.PageSize));
    }

    public async Task<ApiResponse<EmployeeAttendanceDayDto>> GetEmployeeDayAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var day = await _db.EmployeeAttendanceDays.AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.AttendanceDate == date, cancellationToken);
        if (day is null)
            return ApiResponse<EmployeeAttendanceDayDto>.Fail("Attendance day not found.");

        var dto = await AttendanceMapping.MapDayAsync(_db, day.Id, cancellationToken);
        return ApiResponse<EmployeeAttendanceDayDto>.Ok(dto!);
    }

    public async Task<ApiResponse<PagedResult<MonthlyAttendanceEmployeeDto>>> GetMonthlyAsync(
        MonthlyAttendanceQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Month is < 1 or > 12)
            return ApiResponse<PagedResult<MonthlyAttendanceEmployeeDto>>.Fail("Invalid month.");

        var from = new DateOnly(query.Year, query.Month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        var employeesQ = _db.Employees.AsNoTracking().Where(x => x.IsActive);
        if (query.EmployeeId.HasValue) employeesQ = employeesQ.Where(x => x.Id == query.EmployeeId);
        if (query.DepartmentId.HasValue) employeesQ = employeesQ.Where(x => x.DepartmentId == query.DepartmentId);
        if (query.DesignationId.HasValue) employeesQ = employeesQ.Where(x => x.DesignationId == query.DesignationId);

        var total = await employeesQ.CountAsync(cancellationToken);
        var employees = await (
            from e in employeesQ
            join dep in _db.Departments.AsNoTracking() on e.DepartmentId equals dep.Id into deps
            from dep in deps.DefaultIfEmpty()
            orderby e.EmployeeCode
            select new
            {
                e.Id,
                e.EmployeeCode,
                Name = e.FirstName + " " + e.LastName,
                e.DepartmentId,
                DepartmentName = dep != null ? dep.Name : null
            })
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var employeeIds = employees.Select(x => x.Id).ToList();
        var daysQ = _db.EmployeeAttendanceDays.AsNoTracking()
            .Where(x => employeeIds.Contains(x.EmployeeId) && x.AttendanceDate >= from && x.AttendanceDate <= to);

        if (query.ShiftId.HasValue) daysQ = daysQ.Where(x => x.ShiftId == query.ShiftId);
        if (query.Status.HasValue) daysQ = daysQ.Where(x => x.Status == query.Status);

        var days = await daysQ.ToListAsync(cancellationToken);
        var daysByEmployee = days.GroupBy(x => x.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var items = employees.Select(e =>
        {
            daysByEmployee.TryGetValue(e.Id, out var empDays);
            empDays ??= [];
            var cells = new List<DayStatusCellDto>();
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                var day = empDays.FirstOrDefault(x => x.AttendanceDate == d);
                cells.Add(day is null
                    ? new DayStatusCellDto(d, null, 0, 0, 0, false, false)
                    : new DayStatusCellDto(
                        d, day.Status, day.WorkingMinutes, day.LateMinutes, day.OvertimeMinutes,
                        day.IsLate, day.OvertimeMinutes > 0));
            }

            return new MonthlyAttendanceEmployeeDto(
                e.Id, e.EmployeeCode, e.Name, e.DepartmentId, e.DepartmentName,
                cells, AttendanceMapping.BuildSummary(empDays));
        }).ToList();

        return ApiResponse<PagedResult<MonthlyAttendanceEmployeeDto>>.Ok(
            PagedResult<MonthlyAttendanceEmployeeDto>.Create(items, total, query.PageNumber, query.PageSize));
    }

    public async Task<ApiResponse<AttendanceSummaryDto>> GetSummaryAsync(
        Guid employeeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (to < from) return ApiResponse<AttendanceSummaryDto>.Fail("Invalid date range.");

        var days = await _db.EmployeeAttendanceDays.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.AttendanceDate >= from && x.AttendanceDate <= to)
            .ToListAsync(cancellationToken);

        return ApiResponse<AttendanceSummaryDto>.Ok(AttendanceMapping.BuildSummary(days));
    }

    public async Task<ApiResponse<EmployeeAttendanceDayDto>> CorrectAsync(
        string actorUserId,
        ManualAttendanceCorrectionRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return ApiResponse<EmployeeAttendanceDayDto>.Fail("Reason is required.");

        if (await _lockService.IsMonthLockedAsync(request.AttendanceDate.Year, request.AttendanceDate.Month, cancellationToken))
            return ApiResponse<EmployeeAttendanceDayDto>.Fail("Attendance is locked for this month.");

        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.IsActive, cancellationToken);
        if (employee is null)
            return ApiResponse<EmployeeAttendanceDayDto>.Fail("Employee not found or inactive.");

        EmployeeAttendanceDay? day = null;
        string? oldValue = null;

        try
        {
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                day = await _db.EmployeeAttendanceDays
                    .FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.AttendanceDate == request.AttendanceDate, ct);

                if (day?.IsFinalized == true)
                    throw new InvalidOperationException("Attendance day is finalized and cannot be corrected.");

                if (day is null)
                {
                    day = new EmployeeAttendanceDay
                    {
                        EmployeeId = request.EmployeeId,
                        AttendanceDate = request.AttendanceDate
                    };
                    _db.Add(day);
                }
                else
                {
                    oldValue = $"{day.Status}|{day.FirstInUtc:u}|{day.LastOutUtc:u}|{day.WorkingMinutes}";
                    day.UpdatedAtUtc = DateTime.UtcNow;
                }

                day.FirstInUtc = request.FirstInUtc;
                day.LastOutUtc = request.LastOutUtc;
                day.Status = request.Status;
                day.IsManualOverride = true;
                day.Remarks = request.Remarks ?? request.Reason;
                day.AttendanceSource = PunchSource.Admin;

                if (request.WorkingMinutes.HasValue)
                    day.WorkingMinutes = request.WorkingMinutes.Value;
                else if (request.FirstInUtc.HasValue && request.LastOutUtc.HasValue && request.LastOutUtc > request.FirstInUtc)
                    day.WorkingMinutes = (int)Math.Round((request.LastOutUtc.Value - request.FirstInUtc.Value).TotalMinutes);

                if (request.LateMinutes.HasValue) day.LateMinutes = request.LateMinutes.Value;
                if (request.EarlyOutMinutes.HasValue) day.EarlyOutMinutes = request.EarlyOutMinutes.Value;
                if (request.OvertimeMinutes.HasValue) day.OvertimeMinutes = request.OvertimeMinutes.Value;

                day.IsLate = day.LateMinutes > 0;
                day.IsEarlyLeaving = day.EarlyOutMinutes > 0;
                day.HasMissingPunch = false;

                if (day.Id != Guid.Empty)
                    _db.Update(day);

                await _db.SaveChangesAsync(ct);

                await _audit.LogAsync(
                    AttendanceAuditAction.AttendanceCorrected,
                    actorUserId,
                    request.EmployeeId,
                    request.AttendanceDate,
                    oldValue,
                    $"{day.Status}|{day.FirstInUtc:u}|{day.LastOutUtc:u}|{day.WorkingMinutes}",
                    request.Reason,
                    ipAddress,
                    "EmployeeAttendanceDay",
                    day.Id,
                    ct);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<EmployeeAttendanceDayDto>.Fail(ex.Message);
        }

        if (!string.IsNullOrEmpty(employee.UserId))
        {
            await _notifications.NotifyUserAsync(
                employee.UserId,
                "Attendance corrected",
                $"Your attendance for {request.AttendanceDate:yyyy-MM-dd} was corrected.",
                NotificationType.AttendanceCorrected,
                day!.Id.ToString(),
                "EmployeeAttendanceDay",
                cancellationToken);
        }

        var dto = await AttendanceMapping.MapDayAsync(_db, day!.Id, cancellationToken);
        return ApiResponse<EmployeeAttendanceDayDto>.Ok(dto!, "Attendance corrected.");
    }
}
