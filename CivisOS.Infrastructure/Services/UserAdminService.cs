using CivisOS.Application.Common.Models;
using CivisOS.Application.Permissions.DTOs;
using CivisOS.Application.Permissions.Interfaces;
using CivisOS.Domain.Constants;
using CivisOS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CivisOS.Infrastructure.Services;

public class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMemoryCache _cache;

    public UserAdminService(UserManager<ApplicationUser> userManager, IMemoryCache cache)
    {
        _userManager = userManager;
        _cache = cache;
    }

    public async Task<ApiResponse<PagedResult<AdminUserDto>>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        var usersQuery = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            usersQuery = usersQuery.Where(u =>
                (u.Email != null && u.Email.Contains(s))
                || u.FirstName.Contains(s)
                || u.LastName.Contains(s));
        }

        if (query.IsActive.HasValue)
            usersQuery = usersQuery.Where(u => u.IsActive == query.IsActive);

        var users = await usersQuery
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        var dtos = new List<AdminUserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (!string.IsNullOrWhiteSpace(query.Role)
                && !roles.Contains(query.Role, StringComparer.OrdinalIgnoreCase))
                continue;

            dtos.Add(Map(user, roles));
        }

        var total = dtos.Count;
        var page = dtos
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return ApiResponse<PagedResult<AdminUserDto>>.Ok(
            PagedResult<AdminUserDto>.Create(page, total, query.PageNumber, query.PageSize));
    }

    public async Task<ApiResponse<AdminUserDto>> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return ApiResponse<AdminUserDto>.Fail("User not found.");
        var roles = await _userManager.GetRolesAsync(user);
        return ApiResponse<AdminUserDto>.Ok(Map(user, roles));
    }

    public async Task<ApiResponse<AdminUserDto>> CreateAsync(
        string actorUserId,
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = AppRoles.All.FirstOrDefault(r => r.Equals(request.Role, StringComparison.OrdinalIgnoreCase));
        if (role is null) return ApiResponse<AdminUserDto>.Fail("Invalid role.");

        if (role == AppRoles.SuperAdmin)
        {
            var actor = await _userManager.FindByIdAsync(actorUserId);
            if (actor is null) return ApiResponse<AdminUserDto>.Fail("Unauthorized.");
            var actorRoles = await _userManager.GetRolesAsync(actor);
            if (!actorRoles.Contains(AppRoles.SuperAdmin))
                return ApiResponse<AdminUserDto>.Fail("Only SuperAdmin can create SuperAdmin users.");
        }

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null) return ApiResponse<AdminUserDto>.Fail("Email is already registered.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true,
            IsActive = request.IsActive
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return ApiResponse<AdminUserDto>.Fail(
                "Failed to create user.",
                result.Errors.Select(e => e.Description));
        }

        await _userManager.AddToRoleAsync(user, role);
        var roles = await _userManager.GetRolesAsync(user);
        return ApiResponse<AdminUserDto>.Ok(Map(user, roles), "User created.");
    }

    public async Task<ApiResponse<AdminUserDto>> UpdateRolesAsync(
        string actorUserId,
        string userId,
        UpdateUserRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return ApiResponse<AdminUserDto>.Fail("User not found.");

        var desired = (request.Roles ?? [])
            .Select(r => AppRoles.All.FirstOrDefault(a => a.Equals(r, StringComparison.OrdinalIgnoreCase)))
            .Where(r => r is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (desired.Count == 0) return ApiResponse<AdminUserDto>.Fail("At least one valid role is required.");

        if (desired.Contains(AppRoles.SuperAdmin))
        {
            var actor = await _userManager.FindByIdAsync(actorUserId);
            var actorRoles = actor is null ? [] : await _userManager.GetRolesAsync(actor);
            if (!actorRoles.Contains(AppRoles.SuperAdmin))
                return ApiResponse<AdminUserDto>.Fail("Only SuperAdmin can assign SuperAdmin role.");
        }

        var current = await _userManager.GetRolesAsync(user);
        var toRemove = current.Except(desired, StringComparer.OrdinalIgnoreCase).ToList();
        var toAdd = desired.Except(current, StringComparer.OrdinalIgnoreCase).ToList();

        if (toRemove.Count > 0) await _userManager.RemoveFromRolesAsync(user, toRemove);
        if (toAdd.Count > 0) await _userManager.AddToRolesAsync(user, toAdd);

        _cache.Remove($"perms:{userId}");
        var roles = await _userManager.GetRolesAsync(user);
        return ApiResponse<AdminUserDto>.Ok(Map(user, roles), "Roles updated.");
    }

    public async Task<ApiResponse> SetActiveAsync(
        string actorUserId,
        string userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == userId && !isActive)
            return ApiResponse.Fail("You cannot deactivate your own account.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return ApiResponse.Fail("User not found.");

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(AppRoles.SuperAdmin) && !isActive)
            return ApiResponse.Fail("SuperAdmin accounts cannot be deactivated this way.");

        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ApiResponse.Fail("Failed to update user.", result.Errors.Select(e => e.Description));

        _cache.Remove($"perms:{userId}");
        return ApiResponse.Ok(isActive ? "User activated." : "User deactivated.");
    }

    private static AdminUserDto Map(ApplicationUser user, IList<string> roles) => new(
        user.Id,
        user.Email ?? string.Empty,
        user.FirstName,
        user.LastName,
        user.PhoneNumber,
        user.IsActive,
        roles.ToList(),
        user.CreatedAtUtc);
}
