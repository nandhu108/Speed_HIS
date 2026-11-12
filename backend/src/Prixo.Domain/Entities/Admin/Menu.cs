using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

/// <summary>
/// Navigation item. The sidebar is data-driven: /menus/my returns the tree filtered
/// by the caller's permissions (a menu without PermissionCode is visible to everyone).
/// </summary>
public class Menu : BaseEntity
{
    public long? ParentId { get; set; }
    public Menu? Parent { get; set; }
    public required string Title { get; set; }
    public string? Icon { get; set; }
    public string? Path { get; set; }
    public string? PermissionCode { get; set; }
    public int SortOrder { get; set; }

    /// <summary>
    /// Stable seeding key for rows the seeder created (null for admin-created rows).
    /// Lets an admin freely rename/reparent a seeded item without the next seeder run
    /// re-inserting the original as a duplicate — see DbSeeder.EnsureMenuAsync.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// When true, this item is off by default for every role — it only appears in a
    /// role's sidebar once that role has an explicit MenuRoleLayout override with
    /// Hidden = false. Lets a seed create an alternate view of something (e.g. a report
    /// split into one menu item per report) without cluttering every existing role;
    /// an admin opts a specific role in via the same per-role Hide/Show toggle used
    /// everywhere else (MenuService.GetMyMenuAsync/GetTreeForRoleAsync fall back to this
    /// instead of "false" when no override row exists).
    /// </summary>
    public bool HiddenByDefault { get; set; }

    public ICollection<Menu> Children { get; set; } = [];
}
