using CivisOS.Application.Common.Models;
using CivisOS.Application.Employees.DTOs;
using CivisOS.Application.Employees.Interfaces;
using CivisOS.Domain.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IValidator<CreateDepartmentRequest> _createValidator;

    public DepartmentsController(
        IEmployeeService employeeService,
        IValidator<CreateDepartmentRequest> createValidator)
    {
        _employeeService = employeeService;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetDepartmentsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.SocietyAdmin}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<DepartmentDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _employeeService.CreateDepartmentAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
