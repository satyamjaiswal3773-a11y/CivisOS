using CivisOS.Domain.Enums;

namespace CivisOS.Application.Messaging.DTOs;

public record ConversationDto(
    Guid Id,
    string Title,
    ConversationType Type,
    string? CreatedByUserId,
    IReadOnlyList<string> MemberUserIds,
    DateTime? LastMessageAtUtc,
    string? LastMessagePreview,
    DateTime CreatedAtUtc);

public record MessageDto(
    Guid Id,
    Guid ConversationId,
    string SenderUserId,
    string Body,
    DateTime SentAtUtc);

public record CreateConversationRequest(
    string Title,
    ConversationType Type,
    IReadOnlyList<string> MemberUserIds);

public record SendMessageRequest(string Body);

public record ConversationListQuery(
    int PageNumber = 1,
    int PageSize = 20);

public record MessageListQuery(
    int PageNumber = 1,
    int PageSize = 50);
