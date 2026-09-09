using System.Security.Claims;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Messaging.DTOs;
using CivisOS.Application.Messaging.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/conversations")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IMessagingService _messagingService;
    private readonly IValidator<CreateConversationRequest> _createValidator;
    private readonly IValidator<SendMessageRequest> _sendValidator;

    public ConversationsController(
        IMessagingService messagingService,
        IValidator<CreateConversationRequest> createValidator,
        IValidator<SendMessageRequest> sendValidator)
    {
        _messagingService = messagingService;
        _createValidator = createValidator;
        _sendValidator = sendValidator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ConversationDto>>>> GetConversations(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<PagedResult<ConversationDto>>.Fail("Unauthorized."));
        }

        var result = await _messagingService.GetConversationsAsync(
            userId,
            new ConversationListQuery(pageNumber, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> CreateConversation(
        [FromBody] CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<ConversationDto>.Fail("Unauthorized."));
        }

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<ConversationDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _messagingService.CreateConversationAsync(userId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<ApiResponse<PagedResult<MessageDto>>>> GetMessages(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<PagedResult<MessageDto>>.Fail("Unauthorized."));
        }

        var result = await _messagingService.GetMessagesAsync(
            userId,
            id,
            new MessageListQuery(pageNumber, pageSize),
            cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage(
        Guid id,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<MessageDto>.Fail("Unauthorized."));
        }

        var validation = await _sendValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<MessageDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _messagingService.SendMessageAsync(userId, id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
