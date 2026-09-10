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
[Route("api/v1/attendance/exceptions")]
[Authorize]
public class AttendanceExceptionsController : ControllerBase
{
    private readonly IAttendanceExceptionService _exceptionService;
    private readonly IValidator<ResolveExceptionRequest> _resolveValidator;

    public AttendanceExceptionsController(
        IAttendanceExceptionService exceptionService,
        IValidator<ResolveExceptionRequest> resolveValidator)
    {
        _exceptionService = exceptionService;
        _resolveValidator = resolveValidator;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<PagedResult<AttendanceExceptionDto>>>> Get(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] AttendanceExceptionType? exceptionType = null,
        [FromQuery] AttendanceExceptionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _exceptionService.GetAsync(
            new ExceptionListQuery(pageNumber, pageSize, employeeId, from, to, exceptionType, status),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<AttendanceExceptionDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _exceptionService.GetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<AttendanceExceptionDto>>> Resolve(
        Guid id,
        [FromBody] ResolveExceptionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<AttendanceExceptionDto>.Fail("Unauthorized."));
        }

        var validation = await _resolveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AttendanceExceptionDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _exceptionService.ResolveAsync(userId, id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
