using System.Security.Claims;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Leaves.DTOs;
using CivisOS.Application.Leaves.Interfaces;
using CivisOS.Domain.Constants;
using CivisOS.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/leaves")]
[Authorize]
public class LeavesController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly IValidator<CreateLeaveRequestDto> _createValidator;
    private readonly IValidator<LeaveApprovalRequest> _approvalValidator;

    public LeavesController(
        ILeaveService leaveService,
        IValidator<CreateLeaveRequestDto> createValidator,
        IValidator<LeaveApprovalRequest> approvalValidator)
    {
        _leaveService = leaveService;
        _createValidator = createValidator;
        _approvalValidator = approvalValidator;
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor},{AppRoles.Employee},{AppRoles.Driver},{AppRoles.Security}")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Submit(
        [FromBody] CreateLeaveRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<LeaveRequestDto>.Fail("Unauthorized."));
        }

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<LeaveRequestDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _leaveService.SubmitAsync(userId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<PagedResult<LeaveRequestDto>>>> Get(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] LeaveRequestStatus? status = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _leaveService.GetAsync(
            new LeaveListQuery(pageNumber, pageSize, employeeId, status, from, to),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Approve(
        Guid id,
        [FromBody] LeaveApprovalRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<LeaveRequestDto>.Fail("Unauthorized."));
        }

        var validation = await _approvalValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<LeaveRequestDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _leaveService.ApproveAsync(userId, id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> Reject(
        Guid id,
        [FromBody] LeaveApprovalRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<LeaveRequestDto>.Fail("Unauthorized."));
        }

        var validation = await _approvalValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<LeaveRequestDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _leaveService.RejectAsync(userId, id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
