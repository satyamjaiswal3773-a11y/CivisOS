using CivisOS.Application.Auth.DTOs;
using CivisOS.Application.Common.Models;

namespace CivisOS.Application.Common.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponse>> RefreshAsync(RefreshTokenRequest request, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> GetMeAsync(string userId, CancellationToken cancellationToken = default);
}
