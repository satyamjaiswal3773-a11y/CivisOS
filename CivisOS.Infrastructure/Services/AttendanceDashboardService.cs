using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class AttendanceDashboardService : IAttendanceDashboardService
{
    private readonly IApplicationDbContext _db;

    public AttendanceDashboardService(IApplicationDbContext db) => _db = db;

    public async Task<ApiResponse<AttendanceDashboardDto>> GetDashboardAsync(
        DashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var date = query.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var trendFrom = date.AddDays(-6);

        var employeesQ = _db.Employees.AsNoTracking().Where(x => x.IsActive);
        if (query.DepartmentId.HasValue)
            employeesQ = employeesQ.Where(x => x.DepartmentId == query.DepartmentId);

        var totalEmployees = await employeesQ.CountAsync(cancellationToken);
        var employeeIds = await employeesQ.Select(x => x.Id).ToListAsync(cancellationToken);

        var days = await _db.EmployeeAttendanceDays.AsNoTracking()
            .Where(x => x.AttendanceDate == date && employeeIds.Contains(x.EmployeeId))
            .ToListAsync(cancellationToken);

        var present = days.Count(x => x.Status is DayAttendanceStatus.Present or DayAttendanceStatus.Late or DayAttendanceStatus.EarlyLeaving);
        var absent = days.Count(x => x.Status == DayAttendanceStatus.Absent);
        var leave = days.Count(x => x.Status == DayAttendanceStatus.Leave);
        var halfDay = days.Count(x => x.Status == DayAttendanceStatus.HalfDay);
        var weeklyOff = days.Count(x => x.Status == DayAttendanceStatus.WeeklyOff);
        var holiday = days.Count(x => x.Status == DayAttendanceStatus.Holiday);
        var wfh = days.Count(x => x.Status == DayAttendanceStatus.Wfh);
        var onDuty = days.Count(x => x.Status == DayAttendanceStatus.OnDuty);
        var late = days.Count(x => x.IsLate || x.Status == DayAttendanceStatus.Late);
        var earlyLeaving = days.Count(x => x.IsEarlyLeaving || x.Status == DayAttendanceStatus.EarlyLeaving);
        var missingPunch = days.Count(x => x.HasMissingPunch || x.Status == DayAttendanceStatus.MissingPunch);
        var overtime = days.Count(x => x.OvertimeMinutes > 0);

        // Employees without a day record count toward absent for dashboard snapshot
        var covered = days.Select(x => x.EmployeeId).ToHashSet();
        absent += employeeIds.Count(id => !covered.Contains(id));

        var pendingRegularization = await _db.AttendanceRegularizations.AsNoTracking()
            .CountAsync(x => x.Status == AttendanceApprovalStatus.Pending
                && employeeIds.Contains(x.EmployeeId), cancellationToken);

        var pendingApproval = pendingRegularization
            + await _db.OvertimeRequests.AsNoTracking()
                .CountAsync(x => x.Status == AttendanceApprovalStatus.Pending
                    && employeeIds.Contains(x.EmployeeId), cancellationToken)
            + await _db.LeaveRequests.AsNoTracking()
                .CountAsync(x => x.Status == LeaveRequestStatus.Pending
                    && employeeIds.Contains(x.EmployeeId), cancellationToken);

        var trendDays = await _db.EmployeeAttendanceDays.AsNoTracking()
            .Where(x => x.AttendanceDate >= trendFrom && x.AttendanceDate <= date && employeeIds.Contains(x.EmployeeId))
            .ToListAsync(cancellationToken);

        var attendanceTrend = new List<TrendPointDto>();
        var lateTrend = new List<TrendPointDto>();
        var overtimeTrend = new List<TrendPointDto>();
        for (var d = trendFrom; d <= date; d = d.AddDays(1))
        {
            var daySlice = trendDays.Where(x => x.AttendanceDate == d).ToList();
            attendanceTrend.Add(new TrendPointDto(d, daySlice.Count(x =>
                x.Status is DayAttendanceStatus.Present or DayAttendanceStatus.Late or DayAttendanceStatus.EarlyLeaving)));
            lateTrend.Add(new TrendPointDto(d, daySlice.Count(x => x.IsLate)));
            overtimeTrend.Add(new TrendPointDto(d, daySlice.Count(x => x.OvertimeMinutes > 0),
                daySlice.Sum(x => x.OvertimeMinutes)));
        }

        var deptGroups = await (
            from e in employeesQ
            join dep in _db.Departments.AsNoTracking() on e.DepartmentId equals dep.Id
            join d in _db.EmployeeAttendanceDays.AsNoTracking().Where(x => x.AttendanceDate == date)
                on e.Id equals d.EmployeeId into dayJoin
            from d in dayJoin.DefaultIfEmpty()
            group new { e, d, dep } by new { dep.Id, dep.Name } into g
            select new DepartmentAttendanceDto(
                g.Key.Id,
                g.Key.Name,
                g.Count(x => x.d != null && (x.d.Status == DayAttendanceStatus.Present
                    || x.d.Status == DayAttendanceStatus.Late
                    || x.d.Status == DayAttendanceStatus.EarlyLeaving)),
                g.Count(x => x.d == null || x.d.Status == DayAttendanceStatus.Absent),
                g.Count(x => x.d != null && x.d.Status == DayAttendanceStatus.Leave),
                g.Count())
        ).ToListAsync(cancellationToken);

        var leaveUtilization = await (
            from lr in _db.LeaveRequests.AsNoTracking()
            join lt in _db.LeaveTypes.AsNoTracking() on lr.LeaveTypeId equals lt.Id
            where lr.Status == LeaveRequestStatus.Approved
                && lr.FromDate <= date && lr.ToDate >= date.AddDays(-30)
                && employeeIds.Contains(lr.EmployeeId)
            group lr by lt.Name into g
            select new LeaveUtilizationDto(g.Key, g.Sum(x => x.TotalDays))
        ).ToListAsync(cancellationToken);

        var dto = new AttendanceDashboardDto(
            totalEmployees, present, absent, leave, halfDay, weeklyOff, holiday, wfh, onDuty,
            late, earlyLeaving, missingPunch, overtime, pendingRegularization, pendingApproval,
            attendanceTrend, deptGroups, lateTrend, overtimeTrend, leaveUtilization);

        return ApiResponse<AttendanceDashboardDto>.Ok(dto);
    }
}
