using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Unique indexes are partial (WHERE is_deleted = false) so a soft-deleted row
// never blocks reusing its natural key. Schema: admin.

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches", "admin");
        builder.Property(b => b.Code).HasMaxLength(20);
        builder.Property(b => b.Name).HasMaxLength(200);
        builder.Property(b => b.Address).HasMaxLength(500);
        builder.Property(b => b.City).HasMaxLength(100);
        builder.Property(b => b.State).HasMaxLength(100);
        builder.Property(b => b.Pincode).HasMaxLength(10);
        builder.Property(b => b.Phone).HasMaxLength(20);
        builder.Property(b => b.Email).HasMaxLength(200);
        builder.Property(b => b.GstNumber).HasMaxLength(20);
        builder.Property(b => b.ReportHeader).HasMaxLength(1000);
        builder.Property(b => b.TimeZone).HasMaxLength(50);
        builder.Property(b => b.DefaultLandingPath).HasMaxLength(200);
        builder.HasIndex(b => b.Code).IsUnique().HasFilter("is_deleted = false");
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "admin");
        builder.Property(u => u.Username).HasMaxLength(100);
        builder.Property(u => u.PasswordHash).HasMaxLength(500);
        builder.Property(u => u.FullName).HasMaxLength(200);
        builder.Property(u => u.Email).HasMaxLength(200);
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.EmployeeCode).HasMaxLength(50);
        builder.Property(u => u.PhotoPath).HasMaxLength(500);
        builder.HasIndex(u => u.Username).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(u => u.Designation).WithMany()
            .HasForeignKey(u => u.DesignationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(u => u.Department).WithMany()
            .HasForeignKey(u => u.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(u => u.DefaultBranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", "admin");
        builder.Property(r => r.Code).HasMaxLength(50);
        builder.Property(r => r.Name).HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.DefaultLandingPath).HasMaxLength(200);
        builder.HasIndex(r => r.Code).IsUnique().HasFilter("is_deleted = false");
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions", "admin");
        builder.Property(p => p.Code).HasMaxLength(150);
        builder.Property(p => p.Module).HasMaxLength(50);
        builder.Property(p => p.Resource).HasMaxLength(50);
        builder.Property(p => p.Action).HasMaxLength(50);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.HasIndex(p => p.Code).IsUnique().HasFilter("is_deleted = false");
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles", "admin");
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(ur => ur.User).WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(ur => ur.Role).WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions", "admin");
        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(rp => rp.Permission).WithMany()
            .HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RolePresetConfiguration : IEntityTypeConfiguration<RolePreset>
{
    public void Configure(EntityTypeBuilder<RolePreset> builder)
    {
        builder.ToTable("role_presets", "admin");
        builder.Property(p => p.Name).HasMaxLength(100);
        builder.HasIndex(p => p.Name).IsUnique().HasFilter("is_deleted = false");
    }
}

public class RolePresetRoleConfiguration : IEntityTypeConfiguration<RolePresetRole>
{
    public void Configure(EntityTypeBuilder<RolePresetRole> builder)
    {
        builder.ToTable("role_preset_roles", "admin");
        builder.HasIndex(pr => new { pr.RolePresetId, pr.RoleId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(pr => pr.RolePreset).WithMany(p => p.RolePresetRoles)
            .HasForeignKey(pr => pr.RolePresetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(pr => pr.Role).WithMany()
            .HasForeignKey(pr => pr.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserPermissionOverrideConfiguration : IEntityTypeConfiguration<UserPermissionOverride>
{
    public void Configure(EntityTypeBuilder<UserPermissionOverride> builder)
    {
        builder.ToTable("user_permission_overrides", "admin");
        builder.HasIndex(o => new { o.UserId, o.PermissionId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(o => o.User).WithMany()
            .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Permission).WithMany()
            .HasForeignKey(o => o.PermissionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserBranchConfiguration : IEntityTypeConfiguration<UserBranch>
{
    public void Configure(EntityTypeBuilder<UserBranch> builder)
    {
        builder.ToTable("user_branches", "admin");
        builder.HasIndex(ub => new { ub.UserId, ub.BranchId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(ub => ub.User).WithMany(u => u.UserBranches)
            .HasForeignKey(ub => ub.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(ub => ub.Branch).WithMany()
            .HasForeignKey(ub => ub.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DesignationConfiguration : IEntityTypeConfiguration<Designation>
{
    public void Configure(EntityTypeBuilder<Designation> builder)
    {
        builder.ToTable("designations", "admin");
        builder.Property(d => d.Name).HasMaxLength(100);
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.HasIndex(d => d.Name).IsUnique().HasFilter("is_deleted = false");
    }
}

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("menus", "admin");
        builder.Property(m => m.Title).HasMaxLength(100);
        builder.Property(m => m.Icon).HasMaxLength(50);
        builder.Property(m => m.Path).HasMaxLength(200);
        builder.Property(m => m.PermissionCode).HasMaxLength(150);
        builder.Property(m => m.Code).HasMaxLength(300);
        builder.HasIndex(m => m.Code).IsUnique().HasFilter("code IS NOT NULL AND is_deleted = false");
        builder.HasOne(m => m.Parent).WithMany(m => m.Children)
            .HasForeignKey(m => m.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MenuRoleLayoutConfiguration : IEntityTypeConfiguration<MenuRoleLayout>
{
    public void Configure(EntityTypeBuilder<MenuRoleLayout> builder)
    {
        builder.ToTable("menu_role_layouts", "admin");
        builder.Property(l => l.Title).HasMaxLength(100);
        builder.HasIndex(l => new { l.RoleId, l.MenuId }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(l => l.Role).WithMany()
            .HasForeignKey(l => l.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Menu).WithMany()
            .HasForeignKey(l => l.MenuId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Parent).WithMany()
            .HasForeignKey(l => l.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", "admin");
        builder.Property(t => t.TokenHash).HasMaxLength(100);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(100);
        builder.HasIndex(t => t.TokenHash).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(t => t.SessionId);
        builder.HasOne(t => t.User).WithMany()
            .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions", "admin");
        builder.Property(s => s.IpAddress).HasMaxLength(45);
        builder.Property(s => s.UserAgent).HasMaxLength(500);
        builder.HasIndex(s => s.SessionId).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(s => s.UserId);
        builder.HasOne(s => s.User).WithMany()
            .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings", "admin");
        builder.Property(s => s.Key).HasMaxLength(100);
        builder.Property(s => s.Value).HasMaxLength(1000);
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.Group).HasMaxLength(50);
        builder.HasIndex(s => s.Key).IsUnique().HasFilter("is_deleted = false");
    }
}

public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.ToTable("password_histories", "admin");
        builder.Property(p => p.PasswordHash).HasMaxLength(500);
        builder.HasIndex(p => p.UserId);
        builder.HasOne(p => p.User).WithMany()
            .HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
