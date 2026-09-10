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
[Route("api/v1/attendance/days")]
[Authorize]
public class AttendanceDaysController : ControllerBase
{
    private readonly IAttendanceManagementService _managementService;
    private readonly IAttendanceProcessor _processor;
    private readonly IValidator<ProcessAttendanceRequest> _processValidator;
    private readonly IValidator<ManualAttendanceCorrectionRequest> _correctValidator;

    public AttendanceDaysController(
        IAttendanceManagementService managementService,
        IAttendanceProcessor processor,
        IValidator<ProcessAttendanceRequest> processValidator,
        IValidator<ManualAttendanceCorrectionRequest> correctValidator)
    {
        _managementService = managementService;
        _processor = processor;
        _processValidator = processValidator;
        _correctValidator = correctValidator;
    }

    [HttpGet("daily")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeAttendanceDayDto>>>> GetDaily(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateOnly? date = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? designationId = null,
        [FromQuery] Guid? shiftId = null,
        [FromQuery] DayAttendanceStatus? status = null,
        [FromQuery] PunchSource? source = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _managementService.GetDailyAsync(
            new DailyAttendanceQuery(pageNumber, pageSize, date, employeeId, departmentId, designationId, shiftId, status, source),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("employee/{employeeId:guid}/date/{date}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<EmployeeAttendanceDayDto>>> GetEmployeeDay(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var result = await _managementService.GetEmployeeDayAsync(employeeId, date, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("monthly")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<PagedResult<MonthlyAttendanceEmployeeDto>>>> GetMonthly(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? designationId = null,
        [FromQuery] Guid? shiftId = null,
        [FromQuery] DayAttendanceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _managementService.GetMonthlyAsync(
            new MonthlyAttendanceQuery(year, month, pageNumber, pageSize, employeeId, departmentId, designationId, shiftId, status),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<AttendanceSummaryDto>>> GetSummary(
        [FromQuery] Guid employeeId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var result = await _managementService.GetSummaryAsync(employeeId, from, to, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("process")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<int>>> Process(
        [FromBody] ProcessAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _processValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<int>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _processor.ProcessRangeAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("correct")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<EmployeeAttendanceDayDto>>> Correct(
        [FromBody] ManualAttendanceCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<EmployeeAttendanceDayDto>.Fail("Unauthorized."));
        }

        var validation = await _correctValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<EmployeeAttendanceDayDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _managementService.CorrectAsync(userId, request, ip, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
