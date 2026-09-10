using System.Security.Claims;
using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Constants;
using CivisOS.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/attendance/overtime")]
[Authorize]
public class OvertimeController : ControllerBase
{
    private readonly IOvertimeService _overtimeService;
    private readonly IValidator<CreateOvertimeRequest> _createValidator;
    private readonly IValidator<ApproveOvertimeRequest> _approveValidator;

    public OvertimeController(
        IOvertimeService overtimeService,
        IValidator<CreateOvertimeRequest> createValidator,
        IValidator<ApproveOvertimeRequest> approveValidator)
    {
        _overtimeService = overtimeService;
        _createValidator = createValidator;
        _approveValidator = approveValidator;
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor},{AppRoles.Employee},{AppRoles.Driver},{AppRoles.Security}")]
    public async Task<ActionResult<ApiResponse<OvertimeRequestDto>>> Submit(
        [FromBody] CreateOvertimeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<OvertimeRequestDto>.Fail("Unauthorized."));
        }

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<OvertimeRequestDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _overtimeService.SubmitAsync(userId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<PagedResult<OvertimeRequestDto>>>> Get(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] AttendanceApprovalStatus? status = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _overtimeService.GetAsync(
            new OvertimeListQuery(pageNumber, pageSize, employeeId, status, from, to),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<OvertimeRequestDto>>> Approve(
        Guid id,
        [FromBody] ApproveOvertimeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<OvertimeRequestDto>.Fail("Unauthorized."));
        }

        var validation = await _approveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<OvertimeRequestDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _overtimeService.ApproveAsync(userId, id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<OvertimeRequestDto>>> Reject(
        Guid id,
        [FromBody] ApprovalActionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<OvertimeRequestDto>.Fail("Unauthorized."));
        }

        var result = await _overtimeService.RejectAsync(userId, id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
