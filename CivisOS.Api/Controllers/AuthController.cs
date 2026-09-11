using System.Security.Claims;
using CivisOS.Application.Auth.DTOs;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Permissions.DTOs;
using CivisOS.Application.Permissions.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivisOS.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _authService.RegisterAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _refreshValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage)));
        }

        var result = await _authService.RefreshAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<UserDto>.Fail("Unauthorized."));
        }

        var result = await _authService.GetMeAsync(userId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Returns roles, effective permissions, and allowed pages for the React menu.</summary>
    [HttpGet("me/access")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<MyAccessDto>>> MyAccess(
        [FromServices] IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<MyAccessDto>.Fail("Unauthorized."));
        }

        var result = await permissionService.GetMyAccessAsync(userId, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
