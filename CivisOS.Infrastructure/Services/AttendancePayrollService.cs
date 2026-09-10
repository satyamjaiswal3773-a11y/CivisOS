using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class AttendancePayrollService : IAttendancePayrollService
{
    private readonly IApplicationDbContext _db;

    public AttendancePayrollService(IApplicationDbContext db) => _db = db;

    public async Task<ApiResponse<IReadOnlyList<PayrollAttendanceDto>>> GetPayrollDataAsync(
        int year,
        int month,
        Guid? departmentId = null,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        if (month is < 1 or > 12)
            return ApiResponse<IReadOnlyList<PayrollAttendanceDto>>.Fail("Invalid month.");

        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        var employeesQ = _db.Employees.AsNoTracking().Where(x => x.IsActive);
        if (departmentId.HasValue) employeesQ = employeesQ.Where(x => x.DepartmentId == departmentId);
        if (employeeId.HasValue) employeesQ = employeesQ.Where(x => x.Id == employeeId);

        var employees = await employeesQ
            .Select(x => new { x.Id, x.EmployeeCode, Name = x.FirstName + " " + x.LastName })
            .OrderBy(x => x.EmployeeCode)
            .ToListAsync(cancellationToken);

        var employeeIds = employees.Select(x => x.Id).ToList();

        var days = await _db.EmployeeAttendanceDays.AsNoTracking()
            .Where(x => employeeIds.Contains(x.EmployeeId) && x.AttendanceDate >= from && x.AttendanceDate <= to)
            .ToListAsync(cancellationToken);

        var overtime = await _db.OvertimeRequests.AsNoTracking()
            .Where(x => employeeIds.Contains(x.EmployeeId)
                && x.Status == AttendanceApprovalStatus.Approved
                && x.IsPayable
                && x.OvertimeDate >= from && x.OvertimeDate <= to)
            .ToListAsync(cancellationToken);

        var holidays = await _db.HolidayCalendars.AsNoTracking()
            .Where(x => x.IsActive && x.HolidayDate >= from && x.HolidayDate <= to)
            .Select(x => x.HolidayDate)
            .Distinct()
            .ToListAsync(cancellationToken);

        var result = new List<PayrollAttendanceDto>();
        foreach (var emp in employees)
        {
            var empDays = days.Where(x => x.EmployeeId == emp.Id).ToList();
            var summary = AttendanceMapping.BuildSummary(empDays);

            var presentDays = summary.PresentDays;
            var absentDays = summary.AbsentDays;
            var leaveDays = summary.LeaveDays;
            var halfDays = summary.HalfDays;
            var lateMinutes = empDays.Sum(x => x.LateMinutes);
            var approvedOtHours = overtime
                .Where(x => x.EmployeeId == emp.Id)
                .Sum(x => x.ApprovedHours ?? x.RequestedHours);

            var holidayWorkDays = empDays.Count(x =>
                holidays.Contains(x.AttendanceDate)
                && x.WorkingMinutes > 0
                && x.Status == DayAttendanceStatus.Present);

            var weeklyOffWorkDays = empDays.Count(x =>
                x.Status == DayAttendanceStatus.WeeklyOff && x.WorkingMinutes > 0);

            // LOP approximation: absent days not covered by leave/holiday/weekly-off records
            var coveredDates = empDays.Select(x => x.AttendanceDate).ToHashSet();
            var missingDayCount = 0;
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                if (!coveredDates.Contains(d) && !holidays.Contains(d))
                    missingDayCount++;
            }

            var lopDays = absentDays + missingDayCount;

            result.Add(new PayrollAttendanceDto(
                emp.Id,
                emp.EmployeeCode,
                emp.Name,
                year,
                month,
                presentDays,
                absentDays,
                leaveDays,
                lopDays,
                halfDays,
                lateMinutes,
                approvedOtHours,
                holidayWorkDays,
                weeklyOffWorkDays,
                empDays.Sum(x => x.WorkingMinutes)));
        }

        return ApiResponse<IReadOnlyList<PayrollAttendanceDto>>.Ok(result);
    }
}
