using CivisOS.Domain.Enums;

namespace CivisOS.Application.Notifications.DTOs;

public record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    NotificationType Type,
    string? RelatedEntityId,
    string? RelatedEntityType,
    bool IsRead,
    DateTime? ReadAtUtc,
    DateTime CreatedAtUtc);

public record NotificationListQuery(
    int PageNumber = 1,
    int PageSize = 20,
    bool? UnreadOnly = null);

public record RegisterDeviceTokenRequest(
    string DeviceToken,
    string Platform = "web");
