using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Constants;
using CivisOS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/attendance/audit")]
[Authorize]
public class AttendanceAuditController : ControllerBase
{
    private readonly IAttendanceAuditService _auditService;

    public AttendanceAuditController(IAttendanceAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> Get(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] AttendanceAuditAction? action = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _auditService.GetAsync(
            new AuditLogQuery(pageNumber, pageSize, employeeId, action, from, to),
            cancellationToken);
        return Ok(result);
    }
}
