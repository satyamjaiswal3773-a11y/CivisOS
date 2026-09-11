using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CivisOS.Application.Auth.DTOs;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Constants;
using CivisOS.Domain.Entities;
using CivisOS.Infrastructure.Identity;
using CivisOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CivisOS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly JwtSettings _jwt;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IOptions<JwtSettings> jwtOptions)
    {
        _userManager = userManager;
        _db = db;
        _jwt = jwtOptions.Value;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return ApiResponse<AuthResponse>.Fail("Email is already registered.");
        }

        var role = AppRoles.All.First(r => r.Equals(request.Role, StringComparison.OrdinalIgnoreCase));

        // Public self-registration cannot create privileged admin roles.
        var privileged = new[] { AppRoles.SuperAdmin, AppRoles.SocietyAdmin, AppRoles.Supervisor };
        if (privileged.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            return ApiResponse<AuthResponse>.Fail(
                "This role cannot be self-registered. Ask an administrator to create the account via /api/v1/users.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return ApiResponse<AuthResponse>.Fail(
                "Registration failed.",
                result.Errors.Select(e => e.Description));
        }

        await _userManager.AddToRoleAsync(user, role);
        var auth = await BuildAuthResponseAsync(user, null, cancellationToken);
        return ApiResponse<AuthResponse>.Ok(auth, "Registered successfully.");
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return ApiResponse<AuthResponse>.Fail("Invalid email or password.");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return ApiResponse<AuthResponse>.Fail("Invalid email or password.");
        }

        var auth = await BuildAuthResponseAsync(user, ipAddress, cancellationToken);
        return ApiResponse<AuthResponse>.Ok(auth, "Login successful.");
    }

    public async Task<ApiResponse<AuthResponse>> RefreshAsync(RefreshTokenRequest request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        ClaimsPrincipal principal;
        try
        {
            principal = GetPrincipalFromExpiredToken(request.AccessToken);
        }
        catch
        {
            return ApiResponse<AuthResponse>.Fail("Invalid access token.");
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<AuthResponse>.Fail("Invalid access token.");
        }

        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (stored is null || stored.UserId != userId || !stored.IsActive)
        {
            return ApiResponse<AuthResponse>.Fail("Invalid or expired refresh token.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            return ApiResponse<AuthResponse>.Fail("User not found or inactive.");
        }

        stored.RevokedAtUtc = DateTime.UtcNow;
        var newRefresh = CreateRefreshToken(user.Id, ipAddress);
        stored.ReplacedByToken = newRefresh.Token;
        _db.RefreshTokens.Add(newRefresh);
        await _db.SaveChangesAsync(cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expires) = GenerateAccessToken(user, roles);

        return ApiResponse<AuthResponse>.Ok(new AuthResponse(
            accessToken,
            newRefresh.Token,
            expires,
            MapUser(user, roles)), "Token refreshed.");
    }

    public async Task<ApiResponse<UserDto>> GetMeAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ApiResponse<UserDto>.Fail("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return ApiResponse<UserDto>.Ok(MapUser(user, roles));
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user, string? ipAddress, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expires) = GenerateAccessToken(user, roles);
        var refresh = CreateRefreshToken(user.Id, ipAddress);
        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refresh.Token, expires, MapUser(user, roles));
    }

    private (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    private RefreshToken CreateRefreshToken(string userId, string? ipAddress)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays),
            CreatedByIp = ipAddress
        };
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,
            ValidIssuer = _jwt.Issuer,
            ValidAudience = _jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret)),
            ClockSkew = TimeSpan.Zero
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out var securityToken);
        if (securityToken is not JwtSecurityToken jwt ||
            !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }

    private static UserDto MapUser(ApplicationUser user, IList<string> roles) =>
        new(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, user.PhoneNumber, roles.ToList());
}
