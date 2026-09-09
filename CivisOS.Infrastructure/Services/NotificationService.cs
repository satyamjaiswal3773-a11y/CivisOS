using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Notifications.DTOs;
using CivisOS.Application.Notifications.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using CivisOS.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CivisOS.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IFcmPushService _fcm;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IApplicationDbContext db,
        IHubContext<NotificationHub> hub,
        IFcmPushService fcm,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _hub = hub;
        _fcm = fcm;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<NotificationDto>>> GetMyNotificationsAsync(
        string userId,
        NotificationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var rows = _db.AppNotifications.AsNoTracking().Where(n => n.UserId == userId);
        if (query.UnreadOnly == true)
        {
            rows = rows.Where(n => !n.IsRead);
        }

        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Title,
                n.Body,
                n.Type,
                n.RelatedEntityId,
                n.RelatedEntityType,
                n.IsRead,
                n.ReadAtUtc,
                n.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<NotificationDto>>.Ok(
            PagedResult<NotificationDto>.Create(items, total, pageNumber, pageSize));
    }

    public async Task<ApiResponse<NotificationDto>> MarkReadAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var notification = await _db.AppNotifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);
        if (notification is null)
        {
            return ApiResponse<NotificationDto>.Fail("Notification not found.");
        }

        notification.IsRead = true;
        notification.ReadAtUtc = DateTime.UtcNow;
        notification.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(notification);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<NotificationDto>.Ok(new NotificationDto(
            notification.Id,
            notification.Title,
            notification.Body,
            notification.Type,
            notification.RelatedEntityId,
            notification.RelatedEntityType,
            notification.IsRead,
            notification.ReadAtUtc,
            notification.CreatedAtUtc));
    }

    public async Task<ApiResponse> RegisterDeviceAsync(
        string userId,
        RegisterDeviceTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.UserDeviceTokens
            .FirstOrDefaultAsync(t => t.DeviceToken == request.DeviceToken, cancellationToken);

        if (existing is not null)
        {
            existing.UserId = userId;
            existing.Platform = request.Platform;
            existing.IsActive = true;
            existing.LastSeenAtUtc = DateTime.UtcNow;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            _db.Update(existing);
        }
        else
        {
            _db.Add(new UserDeviceToken
            {
                UserId = userId,
                DeviceToken = request.DeviceToken.Trim(),
                Platform = request.Platform.Trim(),
                IsActive = true,
                LastSeenAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Device token registered.");
    }

    public async Task NotifyUserAsync(
        string userId,
        string title,
        string body,
        NotificationType type,
        string? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new AppNotification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType
        };

        _db.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new NotificationDto(
            notification.Id,
            notification.Title,
            notification.Body,
            notification.Type,
            notification.RelatedEntityId,
            notification.RelatedEntityType,
            notification.IsRead,
            notification.ReadAtUtc,
            notification.CreatedAtUtc);

        try
        {
            await _hub.Clients.Group(NotificationHub.UserGroup(userId))
                .SendAsync("notificationReceived", dto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push SignalR notification to user {UserId}", userId);
        }

        var tokens = await _db.UserDeviceTokens.AsNoTracking()
            .Where(t => t.UserId == userId && t.IsActive)
            .Select(t => t.DeviceToken)
            .ToListAsync(cancellationToken);

        if (tokens.Count > 0)
        {
            await _fcm.SendAsync(tokens, title, body, cancellationToken);
        }
    }
}
