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
[Route("api/v1/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IValidator<AttendanceCheckInRequest> _checkInValidator;
    private readonly IValidator<AttendanceCheckOutRequest> _checkOutValidator;

    public AttendanceController(
        IAttendanceService attendanceService,
        IValidator<AttendanceCheckInRequest> checkInValidator,
        IValidator<AttendanceCheckOutRequest> checkOutValidator)
    {
        _attendanceService = attendanceService;
        _checkInValidator = checkInValidator;
        _checkOutValidator = checkOutValidator;
    }

    [HttpPost("check-in")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor},{AppRoles.Employee},{AppRoles.Driver},{AppRoles.Security}")]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> CheckIn(
        [FromBody] AttendanceCheckInRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<AttendanceDto>.Fail("Unauthorized."));
        }

        var validation = await _checkInValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AttendanceDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _attendanceService.CheckInAsync(userId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("check-out")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor},{AppRoles.Employee},{AppRoles.Driver},{AppRoles.Security}")]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> CheckOut(
        [FromBody] AttendanceCheckOutRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<AttendanceDto>.Fail("Unauthorized."));
        }

        var validation = await _checkOutValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AttendanceDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _attendanceService.CheckOutAsync(userId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<PagedResult<AttendanceDto>>>> GetAttendance(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] AttendanceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _attendanceService.GetAttendanceAsync(
            new AttendanceListQuery(pageNumber, pageSize, employeeId, from, to, status),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<PagedResult<AttendanceDto>>>> GetMyAttendance(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] AttendanceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<PagedResult<AttendanceDto>>.Fail("Unauthorized."));
        }

        var result = await _attendanceService.GetMyAttendanceAsync(
            userId,
            new AttendanceListQuery(pageNumber, pageSize, null, from, to, status),
            cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
