using CivisOS.Domain.Enums;

namespace CivisOS.Application.Ai.DTOs;

public record AiAlertDto(
    Guid Id,
    AiAlertType AlertType,
    AiAlertSeverity Severity,
    AiAlertStatus Status,
    string Title,
    string Message,
    string? RelatedEntityId,
    string? RelatedEntityType,
    DateTime DetectedAtUtc,
    DateTime? AcknowledgedAtUtc,
    string? AcknowledgedByUserId,
    DateTime CreatedAtUtc);

public record AiAlertListQuery(
    int PageNumber = 1,
    int PageSize = 20,
    AiAlertStatus? Status = null,
    AiAlertType? AlertType = null);

public record CleaningVerifyRequest(Guid CleaningPhotoId);

public record CleaningVerifyResultDto(
    Guid CleaningPhotoId,
    double ConfidenceScore,
    bool Passed,
    string Summary);

public record AiAssistantAskRequest(string Question);

public record AiAssistantAnswerDto(
    string Question,
    string Answer,
    IReadOnlyList<string> SourcesUsed);
