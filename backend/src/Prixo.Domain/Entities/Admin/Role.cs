using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

public class Role : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    /// <summary>System roles (e.g. SUPER_ADMIN) cannot be renamed or deleted from the UI.</summary>
    public bool IsSystem { get; set; }
    /// <summary>Menu path "/" redirects a user holding this role to (null = no override). Wins over the branch's own DefaultLandingPath when both are set.</summary>
    public string? DefaultLandingPath { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
