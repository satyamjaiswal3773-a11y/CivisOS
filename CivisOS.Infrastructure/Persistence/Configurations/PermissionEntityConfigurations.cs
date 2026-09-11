using CivisOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivisOS.Infrastructure.Persistence.Configurations;

public class AppPermissionConfiguration : IEntityTypeConfiguration<AppPermission>
{
    public void Configure(EntityTypeBuilder<AppPermission> builder)
    {
        builder.ToTable("AppPermissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Module);
        builder.HasIndex(x => x.IsActive);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.RoleName, x.PermissionId }).IsUnique();
        builder.HasIndex(x => x.RoleName);

        builder.HasOne(x => x.Permission)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.PermissionId }).IsUnique();
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.Permission)
            .WithMany(x => x.UserPermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AppPageConfiguration : IEntityTypeConfiguration<AppPage>
{
    public void Configure(EntityTypeBuilder<AppPage> builder)
    {
        builder.ToTable("AppPages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PageKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.RoutePath).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(100);
        builder.Property(x => x.RequiredPermissionCode).HasMaxLength(100);
        builder.HasIndex(x => x.PageKey).IsUnique();
        builder.HasIndex(x => x.SortOrder);
        builder.HasIndex(x => x.IsActive);

        builder.HasOne(x => x.ParentPage)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentPageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
