using CivisOS.Application.Common.Models;
using CivisOS.Application.Employees.DTOs;
using CivisOS.Application.Vehicles.DTOs;
using CivisOS.Application.Vehicles.Interfaces;
using CivisOS.Domain.Constants;
using CivisOS.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;
    private readonly IValidator<CreateVehicleRequest> _createValidator;
    private readonly IValidator<UpdateVehicleRequest> _updateValidator;
    private readonly IValidator<CreateVehicleDocumentRequest> _documentValidator;
    private readonly IValidator<CreateVehicleTypeRequest> _typeValidator;
    private readonly IValidator<AssignVehicleDriverRequest> _driverValidator;

    public VehiclesController(
        IVehicleService vehicleService,
        IValidator<CreateVehicleRequest> createValidator,
        IValidator<UpdateVehicleRequest> updateValidator,
        IValidator<CreateVehicleDocumentRequest> documentValidator,
        IValidator<CreateVehicleTypeRequest> typeValidator,
        IValidator<AssignVehicleDriverRequest> driverValidator)
    {
        _vehicleService = vehicleService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _documentValidator = documentValidator;
        _typeValidator = typeValidator;
        _driverValidator = driverValidator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<VehicleDto>>>> GetVehicles(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] VehicleStatus? status = null,
        [FromQuery] Guid? vehicleTypeId = null,
        [FromQuery] string? department = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _vehicleService.GetVehiclesAsync(
            new VehicleListQuery(pageNumber, pageSize, search, status, vehicleTypeId, department),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<VehicleSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _vehicleService.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("types")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VehicleTypeDto>>>> GetTypes(CancellationToken cancellationToken)
    {
        var result = await _vehicleService.GetTypesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("types")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<VehicleTypeDto>>> CreateType(
        [FromBody] CreateVehicleTypeRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _typeValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<VehicleTypeDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _vehicleService.CreateTypeAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VehicleDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.GetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<VehicleDto>>> Create(
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<VehicleDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _vehicleService.CreateAsync(request, cancellationToken);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result)
            : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<VehicleDto>>> Update(
        Guid id,
        [FromBody] UpdateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<VehicleDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _vehicleService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.DeleteAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("{id:guid}/documents")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<VehicleDocumentDto>>> AddDocument(
        Guid id,
        [FromBody] CreateVehicleDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _documentValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<VehicleDocumentDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _vehicleService.AddDocumentAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}/driver")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin},{AppRoles.Supervisor}")]
    public async Task<ActionResult<ApiResponse<VehicleDto>>> AssignDriver(
        Guid id,
        [FromBody] AssignVehicleDriverRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _driverValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<VehicleDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _vehicleService.AssignDriverAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
