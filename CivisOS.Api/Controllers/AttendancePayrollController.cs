using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/attendance/payroll")]
[Authorize]
public class AttendancePayrollController : ControllerBase
{
    private readonly IAttendancePayrollService _payrollService;

    public AttendancePayrollController(IAttendancePayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PayrollAttendanceDto>>>> GetPayrollData(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _payrollService.GetPayrollDataAsync(year, month, departmentId, employeeId, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
