using System.Security.Claims;
using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/attendance/locks")]
[Authorize]
public class AttendanceLocksController : ControllerBase
{
    private readonly IAttendanceLockService _lockService;
    private readonly IValidator<MonthActionRequest> _monthActionValidator;
    private readonly IValidator<UnlockMonthRequest> _unlockValidator;

    public AttendanceLocksController(
        IAttendanceLockService lockService,
        IValidator<MonthActionRequest> monthActionValidator,
        IValidator<UnlockMonthRequest> unlockValidator)
    {
        _lockService = lockService;
        _monthActionValidator = monthActionValidator;
        _unlockValidator = unlockValidator;
    }

    [HttpPost("finalize")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<AttendanceLockDto>>> Finalize(
        [FromBody] MonthActionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<AttendanceLockDto>.Fail("Unauthorized."));
        }

        var validation = await _monthActionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AttendanceLockDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _lockService.FinalizeMonthAsync(userId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("lock")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<AttendanceLockDto>>> Lock(
        [FromBody] MonthActionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<AttendanceLockDto>.Fail("Unauthorized."));
        }

        var validation = await _monthActionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AttendanceLockDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _lockService.LockMonthAsync(userId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("unlock")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<AttendanceLockDto>>> Unlock(
        [FromBody] UnlockMonthRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<AttendanceLockDto>.Fail("Unauthorized."));
        }

        var validation = await _unlockValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AttendanceLockDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _lockService.UnlockMonthAsync(userId, request, ip, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AttendanceLockDto>>>> GetLocks(
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _lockService.GetLocksAsync(year, cancellationToken);
        return Ok(result);
    }
}
