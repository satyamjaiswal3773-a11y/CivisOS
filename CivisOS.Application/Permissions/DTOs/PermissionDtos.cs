namespace CivisOS.Application.Permissions.DTOs;

public record PermissionDto(
    Guid Id,
    string Code,
    string Name,
    string Module,
    string? Description,
    bool IsActive);

public record RolePermissionMatrixDto(
    string RoleName,
    IReadOnlyList<PermissionDto> AllPermissions,
    IReadOnlyList<string> GrantedPermissionCodes);

public record SetRolePermissionsRequest(IReadOnlyList<string> PermissionCodes);

public record UserPermissionMatrixDto(
    string UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> EffectivePermissionCodes,
    IReadOnlyList<UserPermissionOverrideDto> Overrides);

public record UserPermissionOverrideDto(
    string PermissionCode,
    bool IsGranted);

public record SetUserPermissionsRequest(
    IReadOnlyList<UserPermissionOverrideDto> Overrides);

public record MyAccessDto(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<AppPageDto> Pages);

public record AppPageDto(
    Guid Id,
    string PageKey,
    string Title,
    string RoutePath,
    string? Icon,
    string? RequiredPermissionCode,
    Guid? ParentPageId,
    int SortOrder,
    IReadOnlyList<AppPageDto> Children);

public record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string Role,
    bool IsActive = true);

public record AdminUserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc);

public record UpdateUserRolesRequest(IReadOnlyList<string> Roles);

public record UserListQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    string? Role = null,
    bool? IsActive = null);
