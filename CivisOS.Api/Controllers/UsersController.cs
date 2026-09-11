using System.Security.Claims;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Permissions.DTOs;
using CivisOS.Application.Permissions.Interfaces;
using CivisOS.Domain.Constants;
using CivisOS.Infrastructure.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserAdminService _userAdminService;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRolesRequest> _rolesValidator;

    public UsersController(
        IUserAdminService userAdminService,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRolesRequest> rolesValidator)
    {
        _userAdminService = userAdminService;
        _createValidator = createValidator;
        _rolesValidator = rolesValidator;
    }

    [HttpGet]
    [HasPermission(AppPermissions.UsersView)]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminUserDto>>>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _userAdminService.GetUsersAsync(
            new UserListQuery(pageNumber, pageSize, search, role, isActive),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [HasPermission(AppPermissions.UsersView)]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _userAdminService.GetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [HasPermission(AppPermissions.UsersManage)]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(actorId))
            return Unauthorized(ApiResponse<AdminUserDto>.Fail("Unauthorized."));

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _userAdminService.CreateAsync(actorId, request, cancellationToken);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result)
            : BadRequest(result);
    }

    [HttpPut("{id}/roles")]
    [HasPermission(AppPermissions.UsersManage)]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> UpdateRoles(
        string id,
        [FromBody] UpdateUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(actorId))
            return Unauthorized(ApiResponse<AdminUserDto>.Fail("Unauthorized."));

        var validation = await _rolesValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AdminUserDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _userAdminService.UpdateRolesAsync(actorId, id, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/activate")]
    [HasPermission(AppPermissions.UsersManage)]
    public async Task<ActionResult<ApiResponse>> Activate(string id, CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(actorId))
            return Unauthorized(ApiResponse.Fail("Unauthorized."));

        var result = await _userAdminService.SetActiveAsync(actorId, id, true, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/deactivate")]
    [HasPermission(AppPermissions.UsersManage)]
    public async Task<ActionResult<ApiResponse>> Deactivate(string id, CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(actorId))
            return Unauthorized(ApiResponse.Fail("Unauthorized."));

        var result = await _userAdminService.SetActiveAsync(actorId, id, false, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
