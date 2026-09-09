using System.Security.Claims;
using CivisOS.Application.Ai.DTOs;
using CivisOS.Application.Ai.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Constants;
using CivisOS.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly IValidator<CleaningVerifyRequest> _verifyValidator;
    private readonly IValidator<AiAssistantAskRequest> _askValidator;

    public AiController(
        IAiService aiService,
        IValidator<CleaningVerifyRequest> verifyValidator,
        IValidator<AiAssistantAskRequest> askValidator)
    {
        _aiService = aiService;
        _verifyValidator = verifyValidator;
        _askValidator = askValidator;
    }

    [HttpGet("alerts")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<PagedResult<AiAlertDto>>>> GetAlerts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] AiAlertStatus? status = null,
        [FromQuery] AiAlertType? alertType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _aiService.GetAlertsAsync(
            new AiAlertListQuery(pageNumber, pageSize, status, alertType),
            cancellationToken);
        return Ok(result);
    }

    [HttpPatch("alerts/{id:guid}/ack")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<AiAlertDto>>> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<AiAlertDto>.Fail("Unauthorized."));
        }

        var result = await _aiService.AcknowledgeAlertAsync(userId, id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("cleaning/verify")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<CleaningVerifyResultDto>>> VerifyCleaning(
        [FromBody] CleaningVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _verifyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<CleaningVerifyResultDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _aiService.VerifyCleaningPhotoAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("assistant/ask")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<AiAssistantAnswerDto>>> Ask(
        [FromBody] AiAssistantAskRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<AiAssistantAnswerDto>.Fail("Unauthorized."));
        }

        var validation = await _askValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AiAssistantAnswerDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var result = await _aiService.AskAssistantAsync(userId, roles, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("rules/evaluate")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse>> EvaluateRules(CancellationToken cancellationToken)
    {
        await _aiService.EvaluateRulesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Rule engine evaluated."));
    }

    [HttpPost("alerts/overspeed")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<AiAlertDto>>> SimulateOverspeed(
        [FromQuery] Guid vehicleId,
        [FromQuery] double speedKmh,
        CancellationToken cancellationToken)
    {
        var result = await _aiService.EvaluateOverspeedAsync(vehicleId, speedKmh, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
