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
[Route("api/v1/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly IValidator<SetRolePermissionsRequest> _roleValidator;
    private readonly IValidator<SetUserPermissionsRequest> _userValidator;

    public PermissionsController(
        IPermissionService permissionService,
        IValidator<SetRolePermissionsRequest> roleValidator,
        IValidator<SetUserPermissionsRequest> userValidator)
    {
        _permissionService = permissionService;
        _roleValidator = roleValidator;
        _userValidator = userValidator;
    }

    [HttpGet]
    [HasPermission(AppPermissions.PermissionsManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _permissionService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("roles/{roleName}")]
    [HasPermission(AppPermissions.PermissionsManage)]
    public async Task<ActionResult<ApiResponse<RolePermissionMatrixDto>>> GetRolePermissions(
        string roleName,
        CancellationToken cancellationToken)
    {
        var result = await _permissionService.GetRolePermissionsAsync(roleName, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("roles/{roleName}")]
    [HasPermission(AppPermissions.PermissionsManage)]
    public async Task<ActionResult<ApiResponse<RolePermissionMatrixDto>>> SetRolePermissions(
        string roleName,
        [FromBody] SetRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<RolePermissionMatrixDto>.Fail("Unauthorized."));

        var validation = await _roleValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<RolePermissionMatrixDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _permissionService.SetRolePermissionsAsync(userId, roleName, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("users/{userId}")]
    [HasPermission(AppPermissions.PermissionsManage)]
    public async Task<ActionResult<ApiResponse<UserPermissionMatrixDto>>> GetUserPermissions(
        string userId,
        CancellationToken cancellationToken)
    {
        var result = await _permissionService.GetUserPermissionsAsync(userId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("users/{userId}")]
    [HasPermission(AppPermissions.PermissionsManage)]
    public async Task<ActionResult<ApiResponse<UserPermissionMatrixDto>>> SetUserPermissions(
        string userId,
        [FromBody] SetUserPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(actorId))
            return Unauthorized(ApiResponse<UserPermissionMatrixDto>.Fail("Unauthorized."));

        var validation = await _userValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<UserPermissionMatrixDto>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _permissionService.SetUserPermissionsAsync(actorId, userId, request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
