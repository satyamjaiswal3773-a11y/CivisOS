using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Reports.DTOs;
using CivisOS.Application.Reports.Interfaces;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IApplicationDbContext _db;

    public ReportService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<VehicleReportDto>> GetVehicleReportAsync(
        ReportDateRangeQuery query,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = NormalizeRange(query);

        var vehicles = await _db.Vehicles.AsNoTracking().ToListAsync(cancellationToken);
        var statusCounts = vehicles
            .GroupBy(v => v.Status.ToString())
            .Select(g => new ReportSummaryItem(g.Key, g.Count()))
            .OrderBy(x => x.Key)
            .ToList();

        var locationUpdates = await _db.VehicleLocationHistories.AsNoTracking()
            .CountAsync(h => h.RecordedAtUtc >= from && h.RecordedAtUtc <= to, cancellationToken);

        var fenceEvents = await _db.VehicleGeoFenceEvents.AsNoTracking()
            .Where(e => e.OccurredAtUtc >= from && e.OccurredAtUtc <= to)
            .GroupBy(e => e.VehicleId)
            .Select(g => new { VehicleId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var lastLocations = await _db.VehicleLocations.AsNoTracking()
            .Select(l => new { l.VehicleId, l.RecordedAtUtc })
            .ToListAsync(cancellationToken);

        var items = vehicles.Select(v => new VehicleReportRowDto(
            v.Id,
            v.Number,
            v.Make,
            v.Model,
            v.Status,
            lastLocations.FirstOrDefault(l => l.VehicleId == v.Id)?.RecordedAtUtc,
            fenceEvents.FirstOrDefault(e => e.VehicleId == v.Id)?.Count ?? 0)).ToList();

        return ApiResponse<VehicleReportDto>.Ok(new VehicleReportDto(
            from,
            to,
            statusCounts,
            vehicles.Count(v => v.Status == VehicleStatus.Active || v.Status == VehicleStatus.Available),
            locationUpdates,
            fenceEvents.Sum(e => e.Count),
            items));
    }

    public async Task<ApiResponse<AttendanceReportDto>> GetAttendanceReportAsync(
        ReportDateRangeQuery query,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = NormalizeRange(query);
        var fromDate = DateOnly.FromDateTime(from);
        var toDate = DateOnly.FromDateTime(to);

        var rows = await _db.Attendances.AsNoTracking()
            .Where(a => a.AttendanceDate >= fromDate && a.AttendanceDate <= toDate)
            .Select(a => new
            {
                a.Id,
                a.EmployeeId,
                EmployeeName = a.Employee.FirstName + " " + a.Employee.LastName,
                a.AttendanceDate,
                a.Status,
                GeoFenceName = a.GeoFence.Name
            })
            .OrderByDescending(a => a.AttendanceDate)
            .ToListAsync(cancellationToken);

        var statusCounts = rows
            .GroupBy(a => a.Status.ToString())
            .Select(g => new ReportSummaryItem(g.Key, g.Count()))
            .OrderBy(x => x.Key)
            .ToList();

        var items = rows.Select(a => new AttendanceReportRowDto(
            a.Id,
            a.EmployeeId,
            a.EmployeeName,
            a.AttendanceDate,
            a.Status,
            a.GeoFenceName)).ToList();

        return ApiResponse<AttendanceReportDto>.Ok(new AttendanceReportDto(
            from,
            to,
            statusCounts,
            items.Count,
            items.Count(i => i.Status is AttendanceStatus.Present or AttendanceStatus.CheckedOut),
            items.Count(i => i.Status == AttendanceStatus.Rejected),
            items));
    }

    public async Task<ApiResponse<CleaningReportDto>> GetCleaningReportAsync(
        ReportDateRangeQuery query,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = NormalizeRange(query);

        var logs = await _db.CleaningLogs.AsNoTracking()
            .Where(l => l.StartedAtUtc >= from && l.StartedAtUtc <= to)
            .Select(l => new
            {
                l.Id,
                l.CleaningAreaId,
                CleaningAreaName = l.CleaningArea.Name,
                EmployeeName = l.Employee.FirstName + " " + l.Employee.LastName,
                l.Status,
                l.StartedAtUtc,
                l.CompletedAtUtc,
                PhotoCount = l.Photos.Count
            })
            .OrderByDescending(l => l.StartedAtUtc)
            .ToListAsync(cancellationToken);

        var statusCounts = logs
            .GroupBy(l => l.Status.ToString())
            .Select(g => new ReportSummaryItem(g.Key, g.Count()))
            .OrderBy(x => x.Key)
            .ToList();

        var areasCount = await _db.CleaningAreas.AsNoTracking().CountAsync(cancellationToken);

        var items = logs.Select(l => new CleaningReportRowDto(
            l.Id,
            l.CleaningAreaId,
            l.CleaningAreaName,
            l.EmployeeName,
            l.Status,
            l.StartedAtUtc,
            l.CompletedAtUtc,
            l.PhotoCount)).ToList();

        return ApiResponse<CleaningReportDto>.Ok(new CleaningReportDto(
            from,
            to,
            statusCounts,
            areasCount,
            items.Count,
            items.Count(i => i.Status is CleaningLogStatus.Completed or CleaningLogStatus.Inspected),
            items));
    }

    public async Task<ApiResponse<TaskReportDto>> GetTaskReportAsync(
        ReportDateRangeQuery query,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = NormalizeRange(query);

        var tasks = await _db.WorkTasks.AsNoTracking()
            .Where(t => t.CreatedAtUtc >= from && t.CreatedAtUtc <= to)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Priority,
                t.Status,
                AssigneeName = t.AssigneeEmployee == null
                    ? null
                    : t.AssigneeEmployee.FirstName + " " + t.AssigneeEmployee.LastName,
                t.DeadlineUtc,
                t.CreatedAtUtc
            })
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var statusCounts = tasks
            .GroupBy(t => t.Status.ToString())
            .Select(g => new ReportSummaryItem(g.Key, g.Count()))
            .OrderBy(x => x.Key)
            .ToList();

        var items = tasks.Select(t => new TaskReportRowDto(
            t.Id,
            t.Title,
            t.Priority,
            t.Status,
            t.AssigneeName,
            t.DeadlineUtc,
            t.CreatedAtUtc)).ToList();

        return ApiResponse<TaskReportDto>.Ok(new TaskReportDto(
            from,
            to,
            statusCounts,
            items.Count,
            items.Count(i => i.Status == WorkTaskStatus.Completed),
            items.Count(i => i.Status == WorkTaskStatus.Overdue),
            items));
    }

    private static (DateTime From, DateTime To) NormalizeRange(ReportDateRangeQuery query)
    {
        var to = (query.To ?? DateTime.UtcNow).ToUniversalTime();
        var from = (query.From ?? to.AddDays(-30)).ToUniversalTime();
        if (from > to)
        {
            (from, to) = (to.AddDays(-30), from);
        }

        return (from, to);
    }
}
