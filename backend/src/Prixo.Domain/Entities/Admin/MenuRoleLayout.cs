using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Admin;

/// <summary>
/// A role-scoped override of one Menu item's position — lets one role's sidebar nest/reorder
/// an item differently from the shared global tree (Menu.ParentId/SortOrder) without moving it
/// for every other role. One row per (RoleId, MenuId) a role has actually repositioned;
/// anything without a row here just uses the item's own global position.
/// </summary>
public class MenuRoleLayout : BaseEntity
{
    public long RoleId { get; set; }
    public Role? Role { get; set; }

    public long MenuId { get; set; }
    public Menu? Menu { get; set; }

    /// <summary>Override target parent — another Menu row (possibly one created just for this role's layout).</summary>
    public long? ParentId { get; set; }
    public Menu? Parent { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Role-scoped label override — null means "use the item's own global Title". Lets one role rename a shared menu item (e.g. Front Office relabeling a reused master screen) without touching the global catalog or any other role's sidebar.</summary>
    public string? Title { get; set; }

    /// <summary>Hides this menu item from this role's sidebar only — for items with no PermissionCode (so they'd otherwise be unconditionally visible to every role, e.g. "Dashboard") or that this role simply shouldn't see despite holding the gating permission. The global Menu catalog and every other role's tree stay untouched.</summary>
    public bool Hidden { get; set; }
}
