using System.Security.Claims;
using CivisOS.Application.Attendances.DTOs;
using CivisOS.Application.Attendances.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/attendance/import")]
[Authorize]
public class AttendanceImportController : ControllerBase
{
    private readonly IAttendanceImportService _importService;

    public AttendanceImportController(IAttendanceImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("upload")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ImportBatchDto>>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<ImportBatchDto>.Fail("Unauthorized."));
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<ImportBatchDto>.Fail("A non-empty file is required."));
        }

        await using var stream = file.OpenReadStream();
        var result = await _importService.UploadAndValidateAsync(
            userId,
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{batchId:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<ImportBatchDto>>> GetBatch(
        Guid batchId,
        CancellationToken cancellationToken)
    {
        var result = await _importService.GetBatchAsync(batchId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("{batchId:guid}/confirm")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<ImportConfirmResultDto>>> Confirm(
        Guid batchId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<ImportConfirmResultDto>.Fail("Unauthorized."));
        }

        var result = await _importService.ConfirmAsync(userId, batchId, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
