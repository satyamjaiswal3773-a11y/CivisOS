using CivisOS.Application.Common.Models;
using CivisOS.Application.Reports.DTOs;
using CivisOS.Application.Reports.Interfaces;
using CivisOS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("vehicles")]
    public async Task<ActionResult<ApiResponse<VehicleReportDto>>> Vehicles(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetVehicleReportAsync(new ReportDateRangeQuery(from, to), cancellationToken);
        return Ok(result);
    }

    [HttpGet("attendance")]
    public async Task<ActionResult<ApiResponse<AttendanceReportDto>>> Attendance(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetAttendanceReportAsync(new ReportDateRangeQuery(from, to), cancellationToken);
        return Ok(result);
    }

    [HttpGet("cleaning")]
    public async Task<ActionResult<ApiResponse<CleaningReportDto>>> Cleaning(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetCleaningReportAsync(new ReportDateRangeQuery(from, to), cancellationToken);
        return Ok(result);
    }

    [HttpGet("tasks")]
    public async Task<ActionResult<ApiResponse<TaskReportDto>>> Tasks(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetTaskReportAsync(new ReportDateRangeQuery(from, to), cancellationToken);
        return Ok(result);
    }
}
