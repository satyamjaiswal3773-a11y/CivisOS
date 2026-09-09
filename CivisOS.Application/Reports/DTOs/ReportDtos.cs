using CivisOS.Domain.Enums;

namespace CivisOS.Application.Reports.DTOs;

public record ReportSummaryItem(string Key, int Count);

public record VehicleReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<ReportSummaryItem> StatusCounts,
    int ActiveVehicles,
    int LocationUpdates,
    int FenceEvents,
    IReadOnlyList<VehicleReportRowDto> Items);

public record VehicleReportRowDto(
    Guid VehicleId,
    string Number,
    string Make,
    string Model,
    VehicleStatus Status,
    DateTime? LastLocationAtUtc,
    int FenceEventCount);

public record AttendanceReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<ReportSummaryItem> StatusCounts,
    int TotalRecords,
    int PresentCount,
    int RejectedCount,
    IReadOnlyList<AttendanceReportRowDto> Items);

public record AttendanceReportRowDto(
    Guid AttendanceId,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly AttendanceDate,
    AttendanceStatus Status,
    string GeoFenceName);

public record CleaningReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<ReportSummaryItem> StatusCounts,
    int AreasCount,
    int LogsCount,
    int CompletedLogs,
    IReadOnlyList<CleaningReportRowDto> Items);

public record CleaningReportRowDto(
    Guid LogId,
    Guid CleaningAreaId,
    string CleaningAreaName,
    string EmployeeName,
    CleaningLogStatus Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    int PhotoCount);

public record TaskReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<ReportSummaryItem> StatusCounts,
    int TotalTasks,
    int CompletedTasks,
    int OverdueTasks,
    IReadOnlyList<TaskReportRowDto> Items);

public record TaskReportRowDto(
    Guid TaskId,
    string Title,
    WorkTaskPriority Priority,
    WorkTaskStatus Status,
    string? AssigneeName,
    DateTime? DeadlineUtc,
    DateTime CreatedAtUtc);

public record ReportDateRangeQuery(DateTime? From = null, DateTime? To = null);
