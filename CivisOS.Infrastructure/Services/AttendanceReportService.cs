using System.Text;
using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class AttendanceReportService : IAttendanceReportService
{
    private readonly IApplicationDbContext _db;

    public AttendanceReportService(IApplicationDbContext db) => _db = db;

    public async Task<ApiResponse<PagedResult<EmployeeAttendanceDayDto>>> GetReportAsync(
        string reportType,
        AttendanceReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var fromDate = query.From ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30);
        var toDate = query.To ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (toDate < fromDate) return ApiResponse<PagedResult<EmployeeAttendanceDayDto>>.Fail("Invalid date range.");

        var q =
            from d in _db.EmployeeAttendanceDays.AsNoTracking()
            join e in _db.Employees.AsNoTracking() on d.EmployeeId equals e.Id
            join dep in _db.Departments.AsNoTracking() on e.DepartmentId equals dep.Id into deps
            from dep in deps.DefaultIfEmpty()
            join des in _db.Designations.AsNoTracking() on e.DesignationId equals des.Id into dess
            from des in dess.DefaultIfEmpty()
            join s in _db.ShiftMasters.AsNoTracking() on d.ShiftId equals s.Id into shifts
            from s in shifts.DefaultIfEmpty()
            where d.AttendanceDate >= fromDate && d.AttendanceDate <= toDate
            select new { d, e, dep, des, s };

        if (query.EmployeeId.HasValue) q = q.Where(x => x.d.EmployeeId == query.EmployeeId);
        if (query.DepartmentId.HasValue) q = q.Where(x => x.e.DepartmentId == query.DepartmentId);
        if (query.DesignationId.HasValue) q = q.Where(x => x.e.DesignationId == query.DesignationId);
        if (query.ShiftId.HasValue) q = q.Where(x => x.d.ShiftId == query.ShiftId);
        if (query.Status.HasValue) q = q.Where(x => x.d.Status == query.Status);

        var type = (reportType ?? "daily").Trim().ToLowerInvariant();
        q = type switch
        {
            "late" => q.Where(x => x.d.IsLate || x.d.Status == DayAttendanceStatus.Late),
            "absent" or "absenteeism" => q.Where(x => x.d.Status == DayAttendanceStatus.Absent),
            "missingpunch" or "missing-punch" or "missing_punch" =>
                q.Where(x => x.d.HasMissingPunch || x.d.Status == DayAttendanceStatus.MissingPunch),
            "overtime" or "ot" => q.Where(x => x.d.OvertimeMinutes > 0),
            "earlyleaving" or "early-leaving" or "early_leaving" =>
                q.Where(x => x.d.IsEarlyLeaving || x.d.Status == DayAttendanceStatus.EarlyLeaving),
            "leave" or "leave-vs-attendance" or "leave_vs_attendance" =>
                q.Where(x => x.d.Status == DayAttendanceStatus.Leave || x.d.Status == DayAttendanceStatus.HalfDay),
            "halfday" or "half-day" or "half_day" => q.Where(x => x.d.Status == DayAttendanceStatus.HalfDay),
            "holiday" => q.Where(x => x.d.Status == DayAttendanceStatus.Holiday),
            "weeklyoff" or "weekly-off" or "weekly_off" => q.Where(x => x.d.Status == DayAttendanceStatus.WeeklyOff),
            "present" => q.Where(x => x.d.Status == DayAttendanceStatus.Present
                || x.d.Status == DayAttendanceStatus.Late
                || x.d.Status == DayAttendanceStatus.EarlyLeaving),
            "wfh" => q.Where(x => x.d.Status == DayAttendanceStatus.Wfh),
            "onduty" or "on-duty" or "on_duty" => q.Where(x => x.d.Status == DayAttendanceStatus.OnDuty),
            "shift-wise" or "shift_wise" or "shiftwise" => q,
            "monthly" or "employee" or "department" or "summary" or "daily" => q,
            _ => q
        };

        var sortBy = (query.SortBy ?? "date").Trim().ToLowerInvariant();
        q = (sortBy, query.SortDescending) switch
        {
            ("employeecode", true) => q.OrderByDescending(x => x.e.EmployeeCode),
            ("employeecode", false) => q.OrderBy(x => x.e.EmployeeCode),
            ("status", true) => q.OrderByDescending(x => x.d.Status),
            ("status", false) => q.OrderBy(x => x.d.Status),
            ("workingminutes", true) => q.OrderByDescending(x => x.d.WorkingMinutes),
            ("workingminutes", false) => q.OrderBy(x => x.d.WorkingMinutes),
            ("date", true) => q.OrderByDescending(x => x.d.AttendanceDate).ThenBy(x => x.e.EmployeeCode),
            _ => q.OrderBy(x => x.d.AttendanceDate).ThenBy(x => x.e.EmployeeCode)
        };

        var total = await q.CountAsync(cancellationToken);
        var items = await q
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

    public async Task<ApiResponse<byte[]>> ExportAsync(
        string reportType,
        AttendanceReportQuery query,
        string format,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(format)
            && !string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            return ApiResponse<byte[]>.Fail("Only CSV export is supported.");

        var exportQuery = query with { PageNumber = 1, PageSize = 100 };
        var all = new List<EmployeeAttendanceDayDto>();
        while (true)
        {
            var page = await GetReportAsync(reportType, exportQuery, cancellationToken);
            if (!page.Success || page.Data is null)
                return ApiResponse<byte[]>.Fail(page.Message ?? "Export failed.");

            all.AddRange(page.Data.Items);
            if (!page.Data.HasNextPage) break;
            exportQuery = exportQuery with { PageNumber = exportQuery.PageNumber + 1 };
            if (all.Count > 50000) break;
        }

        var sb = new StringBuilder();
        sb.AppendLine("EmployeeCode,EmployeeName,Department,Designation,Date,Shift,FirstInUtc,LastOutUtc,WorkingMinutes,LateMinutes,EarlyOutMinutes,OvertimeMinutes,Status,IsLate,HasMissingPunch,Remarks");
        foreach (var r in all)
        {
            sb.Append(Escape(r.EmployeeCode)).Append(',')
                .Append(Escape(r.EmployeeName)).Append(',')
                .Append(Escape(r.DepartmentName)).Append(',')
                .Append(Escape(r.DesignationName)).Append(',')
                .Append(r.AttendanceDate.ToString("yyyy-MM-dd")).Append(',')
                .Append(Escape(r.ShiftName)).Append(',')
                .Append(r.FirstInUtc?.ToString("u") ?? "").Append(',')
                .Append(r.LastOutUtc?.ToString("u") ?? "").Append(',')
                .Append(r.WorkingMinutes).Append(',')
                .Append(r.LateMinutes).Append(',')
                .Append(r.EarlyOutMinutes).Append(',')
                .Append(r.OvertimeMinutes).Append(',')
                .Append(r.Status).Append(',')
                .Append(r.IsLate).Append(',')
                .Append(r.HasMissingPunch).Append(',')
                .Append(Escape(r.Remarks))
                .AppendLine();
        }

        return ApiResponse<byte[]>.Ok(Encoding.UTF8.GetBytes(sb.ToString()), "CSV export ready.");
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
