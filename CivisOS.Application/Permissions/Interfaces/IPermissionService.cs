using CivisOS.Application.Common.Models;
using CivisOS.Application.Permissions.DTOs;

namespace CivisOS.Application.Permissions.Interfaces;

public interface IPermissionService
{
    Task<ApiResponse<IReadOnlyList<PermissionDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<RolePermissionMatrixDto>> GetRolePermissionsAsync(string roleName, CancellationToken cancellationToken = default);
    Task<ApiResponse<RolePermissionMatrixDto>> SetRolePermissionsAsync(string actorUserId, string roleName, SetRolePermissionsRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserPermissionMatrixDto>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserPermissionMatrixDto>> SetUserPermissionsAsync(string actorUserId, string userId, SetUserPermissionsRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<MyAccessDto>> GetMyAccessAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetEffectivePermissionCodesAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(string userId, string permissionCode, CancellationToken cancellationToken = default);
    Task EnsureSeededAsync(CancellationToken cancellationToken = default);
}

public interface IUserAdminService
{
    Task<ApiResponse<PagedResult<AdminUserDto>>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<AdminUserDto>> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<AdminUserDto>> CreateAsync(string actorUserId, CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AdminUserDto>> UpdateRolesAsync(string actorUserId, string userId, UpdateUserRolesRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse> SetActiveAsync(string actorUserId, string userId, bool isActive, CancellationToken cancellationToken = default);
}
