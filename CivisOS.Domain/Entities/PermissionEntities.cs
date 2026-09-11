using CivisOS.Domain.Common;

namespace CivisOS.Domain.Entities;

public class AppPermission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}

public class RolePermission : BaseEntity
{
    public string RoleName { get; set; } = string.Empty;
    public Guid PermissionId { get; set; }
    public AppPermission Permission { get; set; } = null!;
}

public class UserPermission : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>True = grant, False = explicit deny (overrides role).</summary>
    public bool IsGranted { get; set; } = true;

    public Guid PermissionId { get; set; }
    public AppPermission Permission { get; set; } = null!;
}

/// <summary>Frontend page/menu metadata mapped to a required permission.</summary>
public class AppPage : BaseEntity
{
    public string PageKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string RoutePath { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? RequiredPermissionCode { get; set; }
    public Guid? ParentPageId { get; set; }
    public AppPage? ParentPage { get; set; }
    public ICollection<AppPage> Children { get; set; } = new List<AppPage>();
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
