using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/attendance/dashboard")]
[Authorize]
public class AttendanceDashboardController : ControllerBase
{
    private readonly IAttendanceDashboardService _dashboardService;

    public AttendanceDashboardController(IAttendanceDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<AttendanceDashboardDto>>> GetDashboard(
        [FromQuery] DateOnly? date = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetDashboardAsync(
            new DashboardQuery(date, departmentId),
            cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
