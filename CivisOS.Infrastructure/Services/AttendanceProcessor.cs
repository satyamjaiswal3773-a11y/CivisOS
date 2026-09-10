using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Holidays.Interfaces;
using CivisOS.Application.Leaves.Interfaces;
using CivisOS.Application.Notifications.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class AttendanceProcessor : IAttendanceProcessor
{
    private readonly IApplicationDbContext _db;
    private readonly ILeaveService _leaveService;
    private readonly IHolidayService _holidayService;
    private readonly IWeeklyOffService _weeklyOffService;
    private readonly INotificationService _notifications;
    private readonly IAttendanceAuditService _audit;

    public AttendanceProcessor(
        IApplicationDbContext db,
        ILeaveService leaveService,
        IHolidayService holidayService,
        IWeeklyOffService weeklyOffService,
        INotificationService notifications,
        IAttendanceAuditService audit)
    {
        _db = db;
        _leaveService = leaveService;
        _holidayService = holidayService;
        _weeklyOffService = weeklyOffService;
        _notifications = notifications;
        _audit = audit;
    }

    public async Task<ApiResponse<EmployeeAttendanceDayDto>> ProcessEmployeeDayAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == employeeId && x.IsActive, cancellationToken);
        if (employee is null)
            return ApiResponse<EmployeeAttendanceDayDto>.Fail("Employee not found or inactive.");

        var lockStatus = await _db.AttendanceLocks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Year == date.Year && x.Month == date.Month, cancellationToken);
        if (lockStatus is { Status: AttendanceLockStatus.Locked or AttendanceLockStatus.Finalized })
            return ApiResponse<EmployeeAttendanceDayDto>.Fail("Attendance is locked for this month.");

        var day = await _db.EmployeeAttendanceDays
            .Include(x => x.Breaks)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.AttendanceDate == date, cancellationToken);

        if (day?.IsFinalized == true)
            return ApiResponse<EmployeeAttendanceDayDto>.Fail("Attendance day is finalized and cannot be reprocessed.");

        var shiftAssignment = await _db.EmployeeShiftAssignments.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        ShiftMaster? shift = null;
        if (shiftAssignment is not null)
        {
            shift = await _db.ShiftMasters.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == shiftAssignment.ShiftId, cancellationToken);
        }

        var (dayStartUtc, dayEndUtc) = GetPunchWindowUtc(date, shift);

        var punches = await _db.AttendancePunches.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.PunchDateTimeUtc >= dayStartUtc && x.PunchDateTimeUtc <= dayEndUtc)
            .OrderBy(x => x.PunchDateTimeUtc)
            .ToListAsync(cancellationToken);

        var (holiday, holidayId) = await _holidayService.IsHolidayAsync(date, employee.DepartmentId, cancellationToken);
        var isWeeklyOff = await _weeklyOffService.IsWeeklyOffAsync(employeeId, employee.DepartmentId, date, cancellationToken);
        var hasLeave = await _leaveService.HasApprovedLeaveAsync(employeeId, date, cancellationToken);
        LeaveRequest? leave = null;
        if (hasLeave)
        {
            leave = await _db.LeaveRequests.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId
                    && x.Status == LeaveRequestStatus.Approved
                    && x.FromDate <= date
                    && x.ToDate >= date, cancellationToken);
        }

        var openExceptions = await _db.AttendanceExceptions
            .Where(x => x.EmployeeId == employeeId && x.AttendanceDate == date && x.Status == AttendanceExceptionStatus.Open)
            .ToListAsync(cancellationToken);
        foreach (var ex in openExceptions)
        {
            _db.Remove(ex);
        }

        var calc = CalculateFromPunches(punches, shift, date);

        if (day is null)
        {
            day = new EmployeeAttendanceDay
            {
                EmployeeId = employeeId,
                AttendanceDate = date
            };
            _db.Add(day);
        }
        else
        {
            foreach (var b in day.Breaks.ToList())
                _db.Remove(b);
            day.Breaks.Clear();
            day.UpdatedAtUtc = DateTime.UtcNow;
        }

        day.ShiftId = shift?.Id;
        day.FirstInUtc = calc.FirstIn;
        day.LastOutUtc = calc.LastOut;
        day.TotalPunches = punches.Count;
        day.BreakMinutes = calc.BreakMinutes;
        day.WorkingMinutes = calc.WorkingMinutes;
        day.LateMinutes = calc.LateMinutes;
        day.EarlyOutMinutes = calc.EarlyOutMinutes;
        day.OvertimeMinutes = calc.OvertimeMinutes;
        day.ExcessBreakMinutes = calc.ExcessBreakMinutes;
        day.IsLate = calc.LateMinutes > 0;
        day.IsEarlyLeaving = calc.EarlyOutMinutes > 0;
        day.HasMissingPunch = calc.HasMissingPunch;
        day.AttendanceSource = punches.Count > 0 ? punches[0].Source : null;
        day.LeaveRequestId = leave?.Id;
        day.HolidayId = holidayId;
        day.IsManualOverride = false;

        if (holiday)
            day.Status = DayAttendanceStatus.Holiday;
        else if (isWeeklyOff && punches.Count == 0)
            day.Status = DayAttendanceStatus.WeeklyOff;
        else if (hasLeave && leave is not null)
            day.Status = leave.IsHalfDay ? DayAttendanceStatus.HalfDay : DayAttendanceStatus.Leave;
        else if (calc.HasMissingPunch && punches.Count > 0)
            day.Status = DayAttendanceStatus.MissingPunch;
        else if (punches.Count == 0)
            day.Status = DayAttendanceStatus.Absent;
        else if (shift is not null && calc.WorkingMinutes > 0 && calc.WorkingMinutes < shift.MinimumWorkingMinutes / 2)
            day.Status = DayAttendanceStatus.HalfDay;
        else if (calc.IsLate && calc.WorkingMinutes > 0)
            day.Status = DayAttendanceStatus.Late;
        else if (calc.IsEarlyLeaving && calc.WorkingMinutes > 0)
            day.Status = DayAttendanceStatus.EarlyLeaving;
        else if (calc.WorkingMinutes > 0)
            day.Status = DayAttendanceStatus.Present;
        else
            day.Status = DayAttendanceStatus.Absent;

        // Holiday/weekly-off work still records present-like metrics but keep holiday/WO status unless worked
        if ((holiday || isWeeklyOff) && calc.WorkingMinutes > 0)
            day.Status = DayAttendanceStatus.Present;

        foreach (var br in calc.Breaks)
        {
            day.Breaks.Add(new AttendanceBreak
            {
                BreakStartUtc = br.Start,
                BreakEndUtc = br.End,
                DurationMinutes = br.Minutes
            });
        }

        foreach (var exceptionType in calc.Exceptions)
        {
            _db.Add(new AttendanceExceptionRecord
            {
                EmployeeId = employeeId,
                AttendanceDate = date,
                ExceptionType = exceptionType.Type,
                Message = exceptionType.Message,
                Status = AttendanceExceptionStatus.Open,
                EmployeeAttendanceDayId = day.Id
            });
        }

        if (shift is null && punches.Count > 0)
        {
            _db.Add(new AttendanceExceptionRecord
            {
                EmployeeId = employeeId,
                AttendanceDate = date,
                ExceptionType = AttendanceExceptionType.InvalidShift,
                Message = "No active shift assignment for this attendance date.",
                Status = AttendanceExceptionStatus.Open,
                EmployeeAttendanceDayId = day.Id
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (calc.HasMissingPunch && !string.IsNullOrEmpty(employee.UserId))
        {
            await _notifications.NotifyUserAsync(
                employee.UserId,
                "Missing punch",
                $"Missing punch detected for {date:yyyy-MM-dd}.",
                NotificationType.MissingPunch,
                day.Id.ToString(),
                "EmployeeAttendanceDay",
                cancellationToken);
        }

        await _audit.LogAsync(
            AttendanceAuditAction.AttendanceProcessed,
            null,
            employeeId,
            date,
            null,
            day.Status.ToString(),
            "Attendance processed",
            null,
            "EmployeeAttendanceDay",
            day.Id,
            cancellationToken);

        var dto = await AttendanceMapping.MapDayAsync(_db, day.Id, cancellationToken);
        return ApiResponse<EmployeeAttendanceDayDto>.Ok(dto!);
    }

    public async Task<ApiResponse<int>> ProcessRangeAsync(ProcessAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        var from = request.From ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var to = request.To ?? from;
        if (to < from) return ApiResponse<int>.Fail("Invalid date range.");

        var employeesQuery = _db.Employees.AsNoTracking().Where(x => x.IsActive);
        if (request.EmployeeId.HasValue)
            employeesQuery = employeesQuery.Where(x => x.Id == request.EmployeeId);
        if (request.DepartmentId.HasValue)
            employeesQuery = employeesQuery.Where(x => x.DepartmentId == request.DepartmentId);

        var employeeIds = await employeesQuery.Select(x => x.Id).ToListAsync(cancellationToken);
        var processed = 0;

        for (var d = from; d <= to; d = d.AddDays(1))
        {
            foreach (var employeeId in employeeIds)
            {
                var result = await ProcessEmployeeDayAsync(employeeId, d, cancellationToken);
                if (result.Success) processed++;
            }
        }

        return ApiResponse<int>.Ok(processed, $"Processed {processed} attendance day record(s).");
    }

    private static (DateTime Start, DateTime End) GetPunchWindowUtc(DateOnly date, ShiftMaster? shift)
    {
        if (shift is null || !shift.IsCrossMidnight)
        {
            var start = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var end = DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(23, 59, 59)), DateTimeKind.Utc);
            return (start, end);
        }

        // Night/cross-midnight: punches from shift start-4h to next day end+4h
        var windowStart = DateTime.SpecifyKind(date.ToDateTime(shift.StartTime).AddHours(-4), DateTimeKind.Utc);
        var windowEnd = DateTime.SpecifyKind(date.AddDays(1).ToDateTime(shift.EndTime).AddHours(4), DateTimeKind.Utc);
        return (windowStart, windowEnd);
    }

    private static PunchCalculation CalculateFromPunches(List<AttendancePunch> punches, ShiftMaster? shift, DateOnly date)
    {
        var result = new PunchCalculation();
        var exceptions = new List<(AttendanceExceptionType Type, string Message)>();

        if (punches.Count == 0)
            return result;

        // Detect duplicates within 60 seconds same type
        for (var i = 1; i < punches.Count; i++)
        {
            if (punches[i].PunchType == punches[i - 1].PunchType
                && (punches[i].PunchDateTimeUtc - punches[i - 1].PunchDateTimeUtc).TotalSeconds < 60)
            {
                exceptions.Add((AttendanceExceptionType.DuplicatePunch,
                    $"Duplicate {punches[i].PunchType} punch at {punches[i].PunchDateTimeUtc:u}."));
            }
        }

        // Validate sequence and pair IN/OUT
        PunchType? expected = PunchType.In;
        var pairs = new List<(DateTime In, DateTime? Out)>();
        DateTime? openIn = null;
        var sequenceValid = true;

        foreach (var punch in punches)
        {
            if (expected == PunchType.In)
            {
                if (punch.PunchType != PunchType.In)
                {
                    sequenceValid = false;
                    exceptions.Add((AttendanceExceptionType.InvalidPunchSequence,
                        $"Expected IN but found OUT at {punch.PunchDateTimeUtc:u}."));
                    continue;
                }
                openIn = punch.PunchDateTimeUtc;
                expected = PunchType.Out;
            }
            else
            {
                if (punch.PunchType != PunchType.Out)
                {
                    sequenceValid = false;
                    exceptions.Add((AttendanceExceptionType.InvalidPunchSequence,
                        $"Expected OUT but found IN at {punch.PunchDateTimeUtc:u}."));
                    // treat as new IN cycle restart
                    if (openIn.HasValue)
                        pairs.Add((openIn.Value, null));
                    openIn = punch.PunchDateTimeUtc;
                    expected = PunchType.Out;
                    continue;
                }
                pairs.Add((openIn!.Value, punch.PunchDateTimeUtc));
                openIn = null;
                expected = PunchType.In;
            }
        }

        if (openIn.HasValue)
        {
            pairs.Add((openIn.Value, null));
            result.HasMissingPunch = true;
            exceptions.Add((AttendanceExceptionType.MissingOut, $"Missing OUT after IN at {openIn:u}."));
        }

        if (punches[0].PunchType == PunchType.Out)
        {
            result.HasMissingPunch = true;
            exceptions.Add((AttendanceExceptionType.MissingIn, "Day starts with OUT punch (missing IN)."));
        }

        var completedPairs = pairs.Where(p => p.Out.HasValue).ToList();
        if (completedPairs.Count == 0 && pairs.Count > 0)
        {
            result.FirstIn = pairs[0].In;
            result.HasMissingPunch = true;
        }
        else if (completedPairs.Count > 0)
        {
            result.FirstIn = pairs.Min(p => p.In);
            result.LastOut = completedPairs.Max(p => p.Out);

            var working = TimeSpan.Zero;
            foreach (var p in completedPairs)
                working += p.Out!.Value - p.In;
            result.WorkingMinutes = (int)Math.Round(working.TotalMinutes);

            // Breaks = gaps between OUT and next IN
            for (var i = 0; i < completedPairs.Count - 1; i++)
            {
                var breakStart = completedPairs[i].Out!.Value;
                var breakEnd = completedPairs[i + 1].In;
                var minutes = (int)Math.Round((breakEnd - breakStart).TotalMinutes);
                if (minutes > 0)
                {
                    result.Breaks.Add((breakStart, breakEnd, minutes));
                    result.BreakMinutes += minutes;
                }
            }
        }

        if (shift is not null && result.FirstIn.HasValue)
        {
            var shiftStart = DateTime.SpecifyKind(date.ToDateTime(shift.StartTime), DateTimeKind.Utc);
            var graceEnd = shiftStart.AddMinutes(shift.GracePeriodMinutes);
            var lateThreshold = shiftStart.AddMinutes(Math.Max(shift.GracePeriodMinutes, shift.LateAfterMinutes));

            if (result.FirstIn > graceEnd)
            {
                result.LateMinutes = (int)Math.Round((result.FirstIn.Value - shiftStart).TotalMinutes) - shift.GracePeriodMinutes;
                if (result.LateMinutes < 0) result.LateMinutes = 0;
                if (result.FirstIn > lateThreshold)
                {
                    result.IsLate = true;
                    exceptions.Add((AttendanceExceptionType.LateArrival,
                        $"Late by {result.LateMinutes} minute(s)."));
                }
            }

            if (result.LastOut.HasValue)
            {
                var shiftEnd = shift.IsCrossMidnight
                    ? DateTime.SpecifyKind(date.AddDays(1).ToDateTime(shift.EndTime), DateTimeKind.Utc)
                    : DateTime.SpecifyKind(date.ToDateTime(shift.EndTime), DateTimeKind.Utc);

                var earlyThreshold = shiftEnd.AddMinutes(-shift.EarlyLeavingAfterMinutes);
                if (result.LastOut < earlyThreshold)
                {
                    result.EarlyOutMinutes = (int)Math.Round((shiftEnd - result.LastOut.Value).TotalMinutes);
                    result.IsEarlyLeaving = true;
                    exceptions.Add((AttendanceExceptionType.EarlyLeaving,
                        $"Early leaving by {result.EarlyOutMinutes} minute(s)."));
                }

                if (shift.OvertimeAllowed)
                {
                    var otStart = shiftEnd.AddMinutes(shift.OvertimeAfterMinutes);
                    if (result.LastOut > otStart)
                    {
                        result.OvertimeMinutes = (int)Math.Round((result.LastOut.Value - shiftEnd).TotalMinutes);
                    }
                }
            }

            if (result.BreakMinutes > shift.AllowedBreakMinutes)
            {
                result.ExcessBreakMinutes = result.BreakMinutes - shift.AllowedBreakMinutes;
                exceptions.Add((AttendanceExceptionType.ExcessBreak,
                    $"Excess break of {result.ExcessBreakMinutes} minute(s)."));
            }
        }

        if (!sequenceValid && exceptions.All(e => e.Type != AttendanceExceptionType.InvalidPunchSequence))
        {
            exceptions.Add((AttendanceExceptionType.InvalidPunchSequence, "Invalid punch sequence detected."));
        }

        result.Exceptions = exceptions;
        return result;
    }

    private sealed class PunchCalculation
    {
        public DateTime? FirstIn { get; set; }
        public DateTime? LastOut { get; set; }
        public int WorkingMinutes { get; set; }
        public int BreakMinutes { get; set; }
        public int ExcessBreakMinutes { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyOutMinutes { get; set; }
        public int OvertimeMinutes { get; set; }
        public bool IsLate { get; set; }
        public bool IsEarlyLeaving { get; set; }
        public bool HasMissingPunch { get; set; }
        public List<(DateTime Start, DateTime End, int Minutes)> Breaks { get; } = [];
        public List<(AttendanceExceptionType Type, string Message)> Exceptions { get; set; } = [];
    }
}

internal static class AttendanceMapping
{
    public static async Task<EmployeeAttendanceDayDto?> MapDayAsync(IApplicationDbContext db, Guid id, CancellationToken cancellationToken)
    {
        return await (
            from d in db.EmployeeAttendanceDays.AsNoTracking()
            join e in db.Employees.AsNoTracking() on d.EmployeeId equals e.Id
            join dep in db.Departments.AsNoTracking() on e.DepartmentId equals dep.Id into deps
            from dep in deps.DefaultIfEmpty()
            join des in db.Designations.AsNoTracking() on e.DesignationId equals des.Id into dess
            from des in dess.DefaultIfEmpty()
            join s in db.ShiftMasters.AsNoTracking() on d.ShiftId equals s.Id into shifts
            from s in shifts.DefaultIfEmpty()
            where d.Id == id
            select new EmployeeAttendanceDayDto(
                d.Id, d.EmployeeId, e.EmployeeCode, e.FirstName + " " + e.LastName,
                e.DepartmentId, dep != null ? dep.Name : null,
                e.DesignationId, des != null ? des.Name : null,
                d.AttendanceDate, d.ShiftId, s != null ? s.ShiftName : null,
                d.FirstInUtc, d.LastOutUtc, d.TotalPunches, d.BreakMinutes, d.WorkingMinutes,
                d.LateMinutes, d.EarlyOutMinutes, d.OvertimeMinutes, d.ExcessBreakMinutes,
                d.Status, d.AttendanceSource, d.IsLate, d.IsEarlyLeaving, d.HasMissingPunch,
                d.IsFinalized, d.IsManualOverride, d.Remarks)
        ).FirstOrDefaultAsync(cancellationToken);
    }

    public static AttendanceSummaryDto BuildSummary(IEnumerable<EmployeeAttendanceDay> days)
    {
        var list = days.ToList();
        return new AttendanceSummaryDto(
            PresentDays: list.Count(x => x.Status is DayAttendanceStatus.Present or DayAttendanceStatus.Late or DayAttendanceStatus.EarlyLeaving),
            AbsentDays: list.Count(x => x.Status == DayAttendanceStatus.Absent),
            LeaveDays: list.Count(x => x.Status == DayAttendanceStatus.Leave) + list.Count(x => x.Status == DayAttendanceStatus.HalfDay) * 0.5m,
            HalfDays: list.Count(x => x.Status == DayAttendanceStatus.HalfDay),
            WeeklyOffDays: list.Count(x => x.Status == DayAttendanceStatus.WeeklyOff),
            HolidayDays: list.Count(x => x.Status == DayAttendanceStatus.Holiday),
            LateCount: list.Count(x => x.IsLate),
            EarlyLeavingCount: list.Count(x => x.IsEarlyLeaving),
            MissingPunchCount: list.Count(x => x.HasMissingPunch || x.Status == DayAttendanceStatus.MissingPunch),
            TotalWorkingMinutes: list.Sum(x => x.WorkingMinutes),
            TotalBreakMinutes: list.Sum(x => x.BreakMinutes),
            TotalOvertimeMinutes: list.Sum(x => x.OvertimeMinutes),
            WfhDays: list.Count(x => x.Status == DayAttendanceStatus.Wfh),
            OnDutyDays: list.Count(x => x.Status == DayAttendanceStatus.OnDuty));
    }
}
