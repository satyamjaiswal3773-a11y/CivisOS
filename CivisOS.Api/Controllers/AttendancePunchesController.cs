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
[Route("api/v1/attendance/punches")]
[Authorize]
public class AttendancePunchesController : ControllerBase
{
    private readonly IAttendancePunchService _punchService;
    private readonly IValidator<CreatePunchRequest> _createValidator;
    private readonly IValidator<SelfPunchRequest> _selfValidator;

    public AttendancePunchesController(
        IAttendancePunchService punchService,
        IValidator<CreatePunchRequest> createValidator,
        IValidator<SelfPunchRequest> selfValidator)
    {
        _punchService = punchService;
        _createValidator = createValidator;
        _selfValidator = selfValidator;
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<AttendancePunchDto>>> CreatePunch(
        [FromBody] CreatePunchRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<AttendancePunchDto>.Fail("Unauthorized."));
        }

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AttendancePunchDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _punchService.CreatePunchAsync(userId, request, ip, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("self")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor},{AppRoles.Employee},{AppRoles.Driver},{AppRoles.Security}")]
    public async Task<ActionResult<ApiResponse<AttendancePunchDto>>> SelfPunch(
        [FromBody] SelfPunchRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<AttendancePunchDto>.Fail("Unauthorized."));
        }

        var validation = await _selfValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AttendancePunchDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _punchService.SelfPunchAsync(userId, request, ip, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<PagedResult<AttendancePunchDto>>>> GetPunches(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] PunchSource? source = null,
        [FromQuery] PunchType? punchType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _punchService.GetPunchesAsync(
            new PunchListQuery(pageNumber, pageSize, employeeId, from, to, source, punchType),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("day")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AttendancePunchDto>>>> GetPunchesForDay(
        [FromQuery] Guid employeeId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var result = await _punchService.GetPunchesForDayAsync(employeeId, date, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
