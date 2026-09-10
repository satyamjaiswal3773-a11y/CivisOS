using CivisOS.Application.Common.Models;
using CivisOS.Application.Leaves.DTOs;
using CivisOS.Application.Leaves.Interfaces;
using CivisOS.Domain.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/leave-types")]
[Authorize]
public class LeaveTypesController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly IValidator<CreateLeaveTypeRequest> _createValidator;
    private readonly IValidator<UpdateLeaveTypeRequest> _updateValidator;

    public LeaveTypesController(
        ILeaveService leaveService,
        IValidator<CreateLeaveTypeRequest> createValidator,
        IValidator<UpdateLeaveTypeRequest> updateValidator)
    {
        _leaveService = leaveService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor},{AppRoles.Employee},{AppRoles.Driver},{AppRoles.Security}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveTypeDto>>>> GetLeaveTypes(
        CancellationToken cancellationToken)
    {
        var result = await _leaveService.GetLeaveTypesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<LeaveTypeDto>>> Create(
        [FromBody] CreateLeaveTypeRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<LeaveTypeDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _leaveService.CreateLeaveTypeAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<LeaveTypeDto>>> Update(
        Guid id,
        [FromBody] UpdateLeaveTypeRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<LeaveTypeDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _leaveService.UpdateLeaveTypeAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
