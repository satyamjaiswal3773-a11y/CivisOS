using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Constants;
using CivisOS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/attendance/reports")]
[Authorize]
public class AttendanceReportsController : ControllerBase
{
    private static readonly HashSet<string> AllowedReportTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "daily",
        "monthly",
        "employee",
        "department",
        "late",
        "early-leaving",
        "absent",
        "half-day",
        "missing-punch",
        "overtime",
        "regularization",
        "exceptions",
        "shift-wise",
        "holiday",
        "weekly-off",
        "summary",
        "absenteeism",
        "leave-vs-attendance"
    };

    private readonly IAttendanceReportService _reportService;

    public AttendanceReportsController(IAttendanceReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("{reportType}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeAttendanceDayDto>>>> GetReport(
        string reportType,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? designationId = null,
        [FromQuery] Guid? shiftId = null,
        [FromQuery] DayAttendanceStatus? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedReportTypes.Contains(reportType))
        {
            return BadRequest(ApiResponse<PagedResult<EmployeeAttendanceDayDto>>.Fail(
                $"Unknown report type '{reportType}'."));
        }

        var result = await _reportService.GetReportAsync(
            reportType,
            new AttendanceReportQuery(
                pageNumber,
                pageSize,
                from,
                to,
                employeeId,
                departmentId,
                designationId,
                shiftId,
                status,
                sortBy,
                sortDescending),
            cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{reportType}/export")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<IActionResult> Export(
        string reportType,
        [FromQuery] string format = "csv",
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? designationId = null,
        [FromQuery] Guid? shiftId = null,
        [FromQuery] DayAttendanceStatus? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedReportTypes.Contains(reportType))
        {
            return BadRequest(ApiResponse<object>.Fail($"Unknown report type '{reportType}'."));
        }

        var result = await _reportService.ExportAsync(
            reportType,
            new AttendanceReportQuery(
                1,
                int.MaxValue,
                from,
                to,
                employeeId,
                departmentId,
                designationId,
                shiftId,
                status,
                sortBy,
                sortDescending),
            format,
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            return BadRequest(result);
        }

        var contentType = format.Equals("csv", StringComparison.OrdinalIgnoreCase)
            ? "text/csv"
            : "application/octet-stream";
        var fileName = $"attendance-{reportType}-{DateTime.UtcNow:yyyyMMddHHmmss}.{format.ToLowerInvariant()}";
        return File(result.Data, contentType, fileName);
    }
}
