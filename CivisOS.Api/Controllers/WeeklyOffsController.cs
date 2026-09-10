using CivisOS.Application.Common.Models;
using CivisOS.Application.Holidays.DTOs;
using CivisOS.Application.Holidays.Interfaces;
using CivisOS.Domain.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/weekly-offs")]
[Authorize]
public class WeeklyOffsController : ControllerBase
{
    private readonly IWeeklyOffService _weeklyOffService;
    private readonly IValidator<CreateWeeklyOffRuleRequest> _createValidator;
    private readonly IValidator<UpdateWeeklyOffRuleRequest> _updateValidator;

    public WeeklyOffsController(
        IWeeklyOffService weeklyOffService,
        IValidator<CreateWeeklyOffRuleRequest> createValidator,
        IValidator<UpdateWeeklyOffRuleRequest> updateValidator)
    {
        _weeklyOffService = weeklyOffService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WeeklyOffRuleDto>>>> GetRules(
        CancellationToken cancellationToken)
    {
        var result = await _weeklyOffService.GetRulesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<WeeklyOffRuleDto>>> Create(
        [FromBody] CreateWeeklyOffRuleRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<WeeklyOffRuleDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _weeklyOffService.CreateAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<WeeklyOffRuleDto>>> Update(
        Guid id,
        [FromBody] UpdateWeeklyOffRuleRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<WeeklyOffRuleDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _weeklyOffService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _weeklyOffService.DeactivateAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
