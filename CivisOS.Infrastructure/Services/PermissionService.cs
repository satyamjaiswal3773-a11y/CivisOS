using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Permissions.DTOs;
using CivisOS.Application.Permissions.Interfaces;
using CivisOS.Domain.Constants;
using CivisOS.Domain.Entities;
using CivisOS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CivisOS.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly IApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMemoryCache _cache;

    public PermissionService(
        IApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IMemoryCache cache)
    {
        _db = db;
        _userManager = userManager;
        _cache = cache;
    }

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        var existingCodes = await _db.AppPermissions.AsNoTracking()
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);
        var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (code, name, module, description) in AppPermissions.Catalog)
        {
            if (existingSet.Contains(code)) continue;
            _db.Add(new AppPermission
            {
                Code = code,
                Name = name,
                Module = module,
                Description = description,
                IsActive = true
            });
        }
        await _db.SaveChangesAsync(cancellationToken);

        var permissions = await _db.AppPermissions.AsNoTracking().ToListAsync(cancellationToken);
        var byCode = permissions.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var role in AppRoles.All)
        {
            var desired = AppPermissions.ForRole(role);
            var existingRolePerms = await _db.RolePermissions
                .Where(x => x.RoleName == role)
                .ToListAsync(cancellationToken);

            // Only seed role permissions if role has none yet (preserve admin customizations)
            if (existingRolePerms.Count > 0) continue;

            foreach (var code in desired)
            {
                if (!byCode.TryGetValue(code, out var perm)) continue;
                _db.Add(new RolePermission { RoleName = role, PermissionId = perm.Id });
            }
        }
        await _db.SaveChangesAsync(cancellationToken);

        if (!await _db.AppPages.AnyAsync(cancellationToken))
        {
            SeedDefaultPages();
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ApiResponse<IReadOnlyList<PermissionDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.AppPermissions.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Module).ThenBy(x => x.Name)
            .Select(x => new PermissionDto(x.Id, x.Code, x.Name, x.Module, x.Description, x.IsActive))
            .ToListAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<PermissionDto>>.Ok(items);
    }

    public async Task<ApiResponse<RolePermissionMatrixDto>> GetRolePermissionsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = NormalizeRole(roleName);
        if (role is null) return ApiResponse<RolePermissionMatrixDto>.Fail("Invalid role.");

        var all = (await GetAllAsync(cancellationToken)).Data ?? [];
        var granted = await _db.RolePermissions.AsNoTracking()
            .Where(x => x.RoleName == role)
            .Join(_db.AppPermissions.AsNoTracking(), rp => rp.PermissionId, p => p.Id, (_, p) => p.Code)
            .ToListAsync(cancellationToken);

        return ApiResponse<RolePermissionMatrixDto>.Ok(new RolePermissionMatrixDto(role, all, granted));
    }

    public async Task<ApiResponse<RolePermissionMatrixDto>> SetRolePermissionsAsync(
        string actorUserId,
        string roleName,
        SetRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = NormalizeRole(roleName);
        if (role is null) return ApiResponse<RolePermissionMatrixDto>.Fail("Invalid role.");
        if (role == AppRoles.SuperAdmin)
            return ApiResponse<RolePermissionMatrixDto>.Fail("SuperAdmin always has all permissions and cannot be edited.");

        var codes = (request.PermissionCodes ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var permissions = await _db.AppPermissions
            .Where(x => x.IsActive && codes.Contains(x.Code))
            .ToListAsync(cancellationToken);

        if (permissions.Count != codes.Count)
            return ApiResponse<RolePermissionMatrixDto>.Fail("One or more permission codes are invalid.");

        await _db.ExecuteInTransactionAsync(async ct =>
        {
            var existing = await _db.RolePermissions.Where(x => x.RoleName == role).ToListAsync(ct);
            foreach (var row in existing) _db.Remove(row);

            foreach (var perm in permissions)
            {
                _db.Add(new RolePermission { RoleName = role, PermissionId = perm.Id });
            }

            await _db.SaveChangesAsync(ct);
        }, cancellationToken);

        InvalidateAllUserCaches();
        return await GetRolePermissionsAsync(role, cancellationToken);
    }

    public async Task<ApiResponse<UserPermissionMatrixDto>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return ApiResponse<UserPermissionMatrixDto>.Fail("User not found.");

        var roles = await _userManager.GetRolesAsync(user);
        var effective = await GetEffectivePermissionCodesAsync(userId, cancellationToken);
        var overrides = await (
            from up in _db.UserPermissions.AsNoTracking()
            join p in _db.AppPermissions.AsNoTracking() on up.PermissionId equals p.Id
            where up.UserId == userId
            select new UserPermissionOverrideDto(p.Code, up.IsGranted)
        ).ToListAsync(cancellationToken);

        return ApiResponse<UserPermissionMatrixDto>.Ok(new UserPermissionMatrixDto(
            user.Id,
            user.Email ?? string.Empty,
            $"{user.FirstName} {user.LastName}".Trim(),
            roles.ToList(),
            effective,
            overrides));
    }

    public async Task<ApiResponse<UserPermissionMatrixDto>> SetUserPermissionsAsync(
        string actorUserId,
        string userId,
        SetUserPermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return ApiResponse<UserPermissionMatrixDto>.Fail("User not found.");

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(AppRoles.SuperAdmin))
            return ApiResponse<UserPermissionMatrixDto>.Fail("Cannot override permissions for SuperAdmin.");

        var overrides = request.Overrides ?? [];
        var codes = overrides.Select(x => x.PermissionCode.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var permissions = await _db.AppPermissions
            .Where(x => x.IsActive && codes.Contains(x.Code))
            .ToListAsync(cancellationToken);
        var byCode = permissions.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        if (byCode.Count != codes.Count)
            return ApiResponse<UserPermissionMatrixDto>.Fail("One or more permission codes are invalid.");

        await _db.ExecuteInTransactionAsync(async ct =>
        {
            var existing = await _db.UserPermissions.Where(x => x.UserId == userId).ToListAsync(ct);
            foreach (var row in existing) _db.Remove(row);

            foreach (var item in overrides)
            {
                if (!byCode.TryGetValue(item.PermissionCode.Trim(), out var perm)) continue;
                _db.Add(new UserPermission
                {
                    UserId = userId,
                    PermissionId = perm.Id,
                    IsGranted = item.IsGranted
                });
            }

            await _db.SaveChangesAsync(ct);
        }, cancellationToken);

        InvalidateUserCache(userId);
        return await GetUserPermissionsAsync(userId, cancellationToken);
    }

    public async Task<ApiResponse<MyAccessDto>> GetMyAccessAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return ApiResponse<MyAccessDto>.Fail("User not found.");

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetEffectivePermissionCodesAsync(userId, cancellationToken);
        var pages = await BuildAllowedPagesAsync(permissions, cancellationToken);

        return ApiResponse<MyAccessDto>.Ok(new MyAccessDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            roles.ToList(),
            permissions,
            pages));
    }

    public async Task<IReadOnlyList<string>> GetEffectivePermissionCodesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"perms:{userId}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<string>? cached) && cached is not null)
            return cached;

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive) return [];

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(AppRoles.SuperAdmin))
        {
            var all = AppPermissions.Catalog.Select(x => x.Code).ToArray();
            _cache.Set(cacheKey, (IReadOnlyList<string>)all, TimeSpan.FromMinutes(5));
            return all;
        }

        var rolePerms = await (
            from rp in _db.RolePermissions.AsNoTracking()
            join p in _db.AppPermissions.AsNoTracking() on rp.PermissionId equals p.Id
            where roles.Contains(rp.RoleName) && p.IsActive
            select p.Code
        ).Distinct().ToListAsync(cancellationToken);

        var set = new HashSet<string>(rolePerms, StringComparer.OrdinalIgnoreCase);

        var overrides = await (
            from up in _db.UserPermissions.AsNoTracking()
            join p in _db.AppPermissions.AsNoTracking() on up.PermissionId equals p.Id
            where up.UserId == userId && p.IsActive
            select new { p.Code, up.IsGranted }
        ).ToListAsync(cancellationToken);

        foreach (var o in overrides)
        {
            if (o.IsGranted) set.Add(o.Code);
            else set.Remove(o.Code);
        }

        var result = set.OrderBy(x => x).ToList();
        _cache.Set(cacheKey, (IReadOnlyList<string>)result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<bool> HasPermissionAsync(string userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permissionCode)) return false;
        var effective = await GetEffectivePermissionCodesAsync(userId, cancellationToken);
        return effective.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyList<AppPageDto>> BuildAllowedPagesAsync(
        IReadOnlyList<string> permissions,
        CancellationToken cancellationToken)
    {
        var pages = await _db.AppPages.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var allowed = pages.Where(p =>
            string.IsNullOrWhiteSpace(p.RequiredPermissionCode)
            || permissions.Contains(p.RequiredPermissionCode, StringComparer.OrdinalIgnoreCase))
            .ToList();

        AppPageDto Map(AppPage p) => new(
            p.Id, p.PageKey, p.Title, p.RoutePath, p.Icon, p.RequiredPermissionCode, p.ParentPageId, p.SortOrder,
            allowed.Where(c => c.ParentPageId == p.Id).Select(Map).ToList());

        return allowed.Where(p => p.ParentPageId == null).Select(Map).ToList();
    }

    private void SeedDefaultPages()
    {
        var dashboard = NewPage("dashboard", "Dashboard", "/dashboard", null, null, 10);
        var employees = NewPage("employees", "Employees", "/employees", AppPermissions.EmployeesView, null, 20);
        var attendance = NewPage("attendance", "Attendance", "/attendance", AppPermissions.AttendanceView, null, 30);
        _db.Add(dashboard);
        _db.Add(employees);
        _db.Add(attendance);

        // Children added after save needs IDs — use fixed Guids
        void AddChild(string key, string title, string route, string? perm, Guid parentId, int sort)
        {
            _db.Add(NewPage(key, title, route, perm, parentId, sort));
        }

        AddChild("attendance-daily", "Daily Attendance", "/attendance/daily", AppPermissions.AttendanceView, attendance.Id, 31);
        AddChild("attendance-monthly", "Monthly Attendance", "/attendance/monthly", AppPermissions.AttendanceView, attendance.Id, 32);
        AddChild("attendance-regularizations", "Regularizations", "/attendance/regularizations", AppPermissions.AttendanceApprove, attendance.Id, 33);
        AddChild("attendance-reports", "Attendance Reports", "/attendance/reports", AppPermissions.AttendanceReports, attendance.Id, 34);
        AddChild("attendance-dashboard", "Attendance Dashboard", "/attendance/dashboard", AppPermissions.AttendanceDashboard, attendance.Id, 35);

        _db.Add(NewPage("shifts", "Shifts", "/shifts", AppPermissions.ShiftsView, null, 40));
        _db.Add(NewPage("leaves", "Leaves", "/leaves", AppPermissions.LeavesView, null, 50));
        _db.Add(NewPage("holidays", "Holidays", "/holidays", AppPermissions.HolidaysManage, null, 55));
        _db.Add(NewPage("users", "Users", "/users", AppPermissions.UsersView, null, 90));
        _db.Add(NewPage("permissions", "Permissions", "/permissions", AppPermissions.PermissionsManage, null, 91));
        _db.Add(NewPage("reports", "Reports", "/reports", AppPermissions.ReportsView, null, 80));
    }

    private static AppPage NewPage(string key, string title, string route, string? perm, Guid? parentId, int sort) => new()
    {
        Id = Guid.NewGuid(),
        PageKey = key,
        Title = title,
        RoutePath = route,
        RequiredPermissionCode = perm,
        ParentPageId = parentId,
        SortOrder = sort,
        IsActive = true
    };

    private static string? NormalizeRole(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return null;
        return AppRoles.All.FirstOrDefault(r => r.Equals(roleName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private void InvalidateUserCache(string userId) => _cache.Remove($"perms:{userId}");

    private void InvalidateAllUserCaches()
    {
        // Simple approach: short TTL already; no global key enumeration needed.
        // Callers that change roles should rely on 5-minute TTL or explicit user invalidation.
    }
}
