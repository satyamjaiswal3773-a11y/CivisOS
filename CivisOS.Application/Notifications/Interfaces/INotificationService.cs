using CivisOS.Application.Common.Models;
using CivisOS.Application.Notifications.DTOs;
using CivisOS.Domain.Enums;

namespace CivisOS.Application.Notifications.Interfaces;

public interface INotificationService
{
    Task<ApiResponse<PagedResult<NotificationDto>>> GetMyNotificationsAsync(string userId, NotificationListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<NotificationDto>> MarkReadAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse> RegisterDeviceAsync(string userId, RegisterDeviceTokenRequest request, CancellationToken cancellationToken = default);
    Task NotifyUserAsync(
        string userId,
        string title,
        string body,
        NotificationType type,
        string? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default);
}
