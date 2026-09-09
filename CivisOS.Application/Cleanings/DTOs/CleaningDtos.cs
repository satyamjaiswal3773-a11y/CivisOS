using CivisOS.Domain.Enums;

namespace CivisOS.Application.Cleanings.DTOs;

public record CleaningAreaDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? GeoFenceId,
    string? GeoFenceName,
    CleaningFrequency Frequency,
    Guid? AssignedEmployeeId,
    string? AssignedEmployeeName,
    DateTime? LastCleanedAtUtc,
    DateTime? NextCleanDueAtUtc,
    CleaningAreaStatus Status,
    DateTime CreatedAtUtc);

public record CreateCleaningAreaRequest(
    string Name,
    string? Description,
    Guid? GeoFenceId,
    CleaningFrequency Frequency,
    Guid? AssignedEmployeeId);

public record UpdateCleaningAreaRequest(
    string Name,
    string? Description,
    Guid? GeoFenceId,
    CleaningFrequency Frequency,
    Guid? AssignedEmployeeId,
    CleaningAreaStatus Status);

public record CleaningAreaListQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    CleaningAreaStatus? Status = null);

public record CleaningScheduleDto(
    Guid Id,
    Guid CleaningAreaId,
    string CleaningAreaName,
    CleaningFrequency Frequency,
    TimeOnly PreferredTimeLocal,
    Guid? AssignedEmployeeId,
    string? AssignedEmployeeName,
    bool IsActive,
    string? Notes,
    DateTime CreatedAtUtc);

public record CreateCleaningScheduleRequest(
    Guid CleaningAreaId,
    CleaningFrequency Frequency,
    TimeOnly PreferredTimeLocal,
    Guid? AssignedEmployeeId,
    string? Notes);

public record CleaningLogDto(
    Guid Id,
    Guid CleaningAreaId,
    string CleaningAreaName,
    Guid EmployeeId,
    string EmployeeName,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    CleaningLogStatus Status,
    string? Notes,
    double? Latitude,
    double? Longitude,
    int? InspectionScore,
    string? InspectionNotes,
    IReadOnlyList<CleaningPhotoDto> Photos,
    DateTime CreatedAtUtc);

public record CleaningPhotoDto(
    Guid Id,
    CleaningPhotoType PhotoType,
    string FilePath,
    string? Caption,
    double? AiConfidenceScore,
    DateTime CreatedAtUtc);

public record CreateCleaningLogRequest(
    Guid CleaningAreaId,
    string? Notes,
    double? Latitude,
    double? Longitude);

public record CleaningLogListQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? CleaningAreaId = null,
    Guid? EmployeeId = null,
    CleaningLogStatus? Status = null,
    DateTime? From = null,
    DateTime? To = null);

public record CompleteCleaningLogRequest(string? Notes);
