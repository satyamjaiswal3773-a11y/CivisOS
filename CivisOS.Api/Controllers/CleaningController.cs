using System.Security.Claims;
using CivisOS.Application.Cleanings.DTOs;
using CivisOS.Application.Cleanings.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Constants;
using CivisOS.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/cleaning")]
[Authorize]
public class CleaningController : ControllerBase
{
    private readonly ICleaningService _cleaningService;
    private readonly IValidator<CreateCleaningAreaRequest> _createAreaValidator;
    private readonly IValidator<UpdateCleaningAreaRequest> _updateAreaValidator;
    private readonly IValidator<CreateCleaningScheduleRequest> _createScheduleValidator;
    private readonly IValidator<CreateCleaningLogRequest> _createLogValidator;

    public CleaningController(
        ICleaningService cleaningService,
        IValidator<CreateCleaningAreaRequest> createAreaValidator,
        IValidator<UpdateCleaningAreaRequest> updateAreaValidator,
        IValidator<CreateCleaningScheduleRequest> createScheduleValidator,
        IValidator<CreateCleaningLogRequest> createLogValidator)
    {
        _cleaningService = cleaningService;
        _createAreaValidator = createAreaValidator;
        _updateAreaValidator = updateAreaValidator;
        _createScheduleValidator = createScheduleValidator;
        _createLogValidator = createLogValidator;
    }

    [HttpGet("areas")]
    public async Task<ActionResult<ApiResponse<PagedResult<CleaningAreaDto>>>> GetAreas(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] CleaningAreaStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _cleaningService.GetAreasAsync(
            new CleaningAreaListQuery(pageNumber, pageSize, search, status),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("areas/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CleaningAreaDto>>> GetAreaById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cleaningService.GetAreaByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("areas")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<CleaningAreaDto>>> CreateArea(
        [FromBody] CreateCleaningAreaRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createAreaValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<CleaningAreaDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _cleaningService.CreateAreaAsync(request, cancellationToken);
        return result.Success
            ? CreatedAtAction(nameof(GetAreaById), new { id = result.Data!.Id }, result)
            : BadRequest(result);
    }

    [HttpPut("areas/{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<CleaningAreaDto>>> UpdateArea(
        Guid id,
        [FromBody] UpdateCleaningAreaRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateAreaValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<CleaningAreaDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _cleaningService.UpdateAreaAsync(id, request, cancellationToken);
        if (!result.Success)
        {
            return result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("schedules")]
    public async Task<ActionResult<ApiResponse<PagedResult<CleaningScheduleDto>>>> GetSchedules(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? cleaningAreaId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _cleaningService.GetSchedulesAsync(pageNumber, pageSize, cleaningAreaId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("schedules")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<CleaningScheduleDto>>> CreateSchedule(
        [FromBody] CreateCleaningScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createScheduleValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<CleaningScheduleDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _cleaningService.CreateScheduleAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("logs")]
    public async Task<ActionResult<ApiResponse<PagedResult<CleaningLogDto>>>> GetLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? cleaningAreaId = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] CleaningLogStatus? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _cleaningService.GetLogsAsync(
            new CleaningLogListQuery(pageNumber, pageSize, cleaningAreaId, employeeId, status, from, to),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("logs")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor},{AppRoles.Employee}")]
    public async Task<ActionResult<ApiResponse<CleaningLogDto>>> CreateLog(
        [FromBody] CreateCleaningLogRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<CleaningLogDto>.Fail("Unauthorized."));
        }

        var validation = await _createLogValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<CleaningLogDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _cleaningService.CreateLogAsync(userId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("logs/{id:guid}/complete")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor},{AppRoles.Employee}")]
    public async Task<ActionResult<ApiResponse<CleaningLogDto>>> CompleteLog(
        Guid id,
        [FromBody] CompleteCleaningLogRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _cleaningService.CompleteLogAsync(id, request ?? new CompleteCleaningLogRequest(null), cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("logs/{id:guid}/photos")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor},{AppRoles.Employee}")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ApiResponse<CleaningPhotoDto>>> AddPhoto(
        Guid id,
        [FromForm] CleaningPhotoType photoType,
        IFormFile file,
        [FromForm] string? caption,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<CleaningPhotoDto>.Fail("Photo file is required."));
        }

        await using var stream = file.OpenReadStream();
        var result = await _cleaningService.AddPhotoAsync(
            id,
            photoType,
            stream,
            file.FileName,
            file.ContentType,
            caption,
            cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
