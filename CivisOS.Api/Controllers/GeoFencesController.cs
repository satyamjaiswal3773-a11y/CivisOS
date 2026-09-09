using CivisOS.Application.Common.Models;
using CivisOS.Application.GeoFences.DTOs;
using CivisOS.Application.GeoFences.Interfaces;
using CivisOS.Domain.Constants;
using CivisOS.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/geofences")]
[Authorize]
public class GeoFencesController : ControllerBase
{
    private readonly IGeoFenceService _geoFenceService;
    private readonly IValidator<CreateGeoFenceRequest> _createValidator;
    private readonly IValidator<UpdateGeoFenceRequest> _updateValidator;
    private readonly IValidator<GeoFenceCheckRequest> _checkValidator;

    public GeoFencesController(
        IGeoFenceService geoFenceService,
        IValidator<CreateGeoFenceRequest> createValidator,
        IValidator<UpdateGeoFenceRequest> updateValidator,
        IValidator<GeoFenceCheckRequest> checkValidator)
    {
        _geoFenceService = geoFenceService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _checkValidator = checkValidator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<GeoFenceDto>>>> GetGeoFences(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] GeoFenceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _geoFenceService.GetGeoFencesAsync(
            new GeoFenceListQuery(pageNumber, pageSize, search, status),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<GeoFenceDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _geoFenceService.GetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<GeoFenceDto>>> Create(
        [FromBody] CreateGeoFenceRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<GeoFenceDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _geoFenceService.CreateAsync(request, cancellationToken);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result)
            : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<GeoFenceDto>>> Update(
        Guid id,
        [FromBody] UpdateGeoFenceRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<GeoFenceDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _geoFenceService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _geoFenceService.DeleteAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("check")]
    public async Task<ActionResult<ApiResponse<GeoFenceCheckResultDto>>> Check(
        [FromBody] GeoFenceCheckRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _checkValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<GeoFenceCheckResultDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _geoFenceService.CheckAsync(request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
