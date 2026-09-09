using System.Security.Claims;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Notifications.DTOs;
using CivisOS.Application.Notifications.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IValidator<RegisterDeviceTokenRequest> _deviceValidator;

    public NotificationsController(
        INotificationService notificationService,
        IValidator<RegisterDeviceTokenRequest> deviceValidator)
    {
        _notificationService = notificationService;
        _deviceValidator = deviceValidator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetMyNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? unreadOnly = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<PagedResult<NotificationDto>>.Fail("Unauthorized."));
        }

        var result = await _notificationService.GetMyNotificationsAsync(
            userId,
            new NotificationListQuery(pageNumber, pageSize, unreadOnly),
            cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<NotificationDto>.Fail("Unauthorized."));
        }

        var result = await _notificationService.MarkReadAsync(userId, id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("devices")]
    public async Task<ActionResult<ApiResponse>> RegisterDevice(
        [FromBody] RegisterDeviceTokenRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse.Fail("Unauthorized."));
        }

        var validation = await _deviceValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _notificationService.RegisterDeviceAsync(userId, request, cancellationToken);
        return Ok(result);
    }
}
