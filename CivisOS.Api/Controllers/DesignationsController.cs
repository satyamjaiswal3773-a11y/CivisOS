using CivisOS.Application.Common.Models;
using CivisOS.Application.Employees.DTOs;
using CivisOS.Application.Employees.Interfaces;
using CivisOS.Domain.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/designations")]
[Authorize]
public class DesignationsController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IValidator<CreateDesignationRequest> _createValidator;

    public DesignationsController(
        IEmployeeService employeeService,
        IValidator<CreateDesignationRequest> createValidator)
    {
        _employeeService = employeeService;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DesignationDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetDesignationsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<DesignationDto>>> Create(
        [FromBody] CreateDesignationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<DesignationDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _employeeService.CreateDesignationAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
