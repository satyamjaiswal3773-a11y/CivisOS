using CivisOS.Domain.Enums;

namespace CivisOS.Application.Tasks.DTOs;

public record WorkTaskDto(
    Guid Id,
    string Title,
    string? Description,
    WorkTaskPriority Priority,
    WorkTaskStatus Status,
    DateTime? DeadlineUtc,
    Guid? AssigneeEmployeeId,
    string? AssigneeEmployeeName,
    Guid? AssignedByEmployeeId,
    string? AssignedByEmployeeName,
    Guid? GeoFenceId,
    string? GeoFenceName,
    Guid? CleaningAreaId,
    string? CleaningAreaName,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? ApprovedAtUtc,
    string? StatusNotes,
    DateTime CreatedAtUtc);

public record CreateWorkTaskRequest(
    string Title,
    string? Description,
    WorkTaskPriority Priority,
    DateTime? DeadlineUtc,
    Guid? AssigneeEmployeeId,
    Guid? GeoFenceId,
    Guid? CleaningAreaId);

public record UpdateWorkTaskRequest(
    string Title,
    string? Description,
    WorkTaskPriority Priority,
    DateTime? DeadlineUtc,
    Guid? GeoFenceId,
    Guid? CleaningAreaId);

public record AssignWorkTaskRequest(Guid AssigneeEmployeeId);

public record UpdateWorkTaskStatusRequest(WorkTaskStatus Status, string? Notes);

public record StartWorkTaskRequest(double Latitude, double Longitude);

public record WorkTaskListQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    WorkTaskStatus? Status = null,
    Guid? AssigneeEmployeeId = null,
    WorkTaskPriority? Priority = null);
