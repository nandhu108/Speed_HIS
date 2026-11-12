using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Auth;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Admin.Menus;

public record MenuNodeDto(long Id, long? ParentId, string Title, string? Icon, string? Path,
    string? PermissionCode, int SortOrder, bool IsActive, bool IsOverridden, bool IsHidden,
    bool HiddenByDefault, List<MenuNodeDto> Children);
public record SaveMenuRequest(long? ParentId, string Title, string? Icon, string? Path,
    string? PermissionCode, int SortOrder, bool IsActive = true, bool HiddenByDefault = false);
/// <summary>Title null means "use this menu item's own global Title" — only pass a value to give this role its own label for the item. Hidden hides it from this role's sidebar only, even if it has no PermissionCode (unconditionally-visible items like "Dashboard") or this role holds the gating permission.</summary>
public record SetMenuPositionRequest(long? ParentId, int SortOrder, string? Title = null, bool Hidden = false);

public class SaveMenuRequestValidator : AbstractValidator<SaveMenuRequest>
{
    public SaveMenuRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Path).MaximumLength(200);
    }
}

public class SetMenuPositionRequestValidator : AbstractValidator<SetMenuPositionRequest>
{
    public SetMenuPositionRequestValidator()
    {
        RuleFor(x => x.ParentId).GreaterThan(0).When(x => x.ParentId is not null);
    }
}

public class MenuService(IAppDbContext db, PermissionService permissions, ICurrentUser currentUser)
{
    /// <summary>Full tree for the global admin screen (includes inactive), raw global positions.</summary>
    public async Task<IReadOnlyList<MenuNodeDto>> GetTreeAsync(CancellationToken ct = default)
    {
        var menus = await db.Menus.AsNoTracking().OrderBy(m => m.SortOrder).ToListAsync(ct);
        return BuildTree(menus);
    }

    /// <summary>
    /// Full tree (includes inactive) with this role's own position overrides applied — powers
    /// the "By menu" tab of a role's Permissions screen. Items this role hasn't repositioned
    /// keep their global position; the underlying Menu catalog is untouched either way.
    /// </summary>
    public async Task<IReadOnlyList<MenuNodeDto>> GetTreeForRoleAsync(long roleId, CancellationToken ct = default)
    {
        if (!await db.Roles.AnyAsync(r => r.Id == roleId, ct))
        {
            throw new NotFoundException(nameof(Role), roleId);
        }

        var menus = await db.Menus.AsNoTracking().OrderBy(m => m.SortOrder).ToListAsync(ct);
        var overrides = await db.MenuRoleLayouts.AsNoTracking()
            .Where(l => l.RoleId == roleId)
            .ToDictionaryAsync(l => l.MenuId, ct);

        return BuildTree(menus, m =>
            overrides.TryGetValue(m.Id, out var o)
                ? (o.ParentId, o.SortOrder, o.Title, o.Hidden, true)
                : (m.ParentId, m.SortOrder, null, m.HiddenByDefault, false));
    }

    /// <summary>
    /// The caller's actual sidebar: active menus whose permission the user holds, positioned
    /// per their roles' own layout overrides (earliest-assigned role's override wins on a
    /// conflict between two of the user's roles — same precedence as AuthService's
    /// DefaultLandingPath resolution).
    /// </summary>
    public async Task<IReadOnlyList<MenuNodeDto>> GetMyMenuAsync(CancellationToken ct = default)
    {
        var menus = await db.Menus.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);

        var userPermissions = await permissions.GetUserPermissionsAsync(currentUser.UserId, ct);
        var allowed = menus
            .Where(m => m.PermissionCode is null || userPermissions.Contains(m.PermissionCode))
            .ToList();

        var roleIds = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == currentUser.UserId && ur.Role.IsActive)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        var overridesByMenuId = new Dictionary<long, MenuRoleLayout>();
        if (roleIds.Count > 0)
        {
            var overrides = await db.MenuRoleLayouts.AsNoTracking()
                .Where(l => roleIds.Contains(l.RoleId))
                .OrderBy(l => l.RoleId)
                .ToListAsync(ct);
            foreach (var o in overrides)
            {
                overridesByMenuId.TryAdd(o.MenuId, o);
            }
        }

        var tree = BuildTree(allowed, m =>
            overridesByMenuId.TryGetValue(m.Id, out var o)
                ? (o.ParentId, o.SortOrder, o.Title, o.Hidden, true)
                : (m.ParentId, m.SortOrder, null, m.HiddenByDefault, false));
        return PruneEmptyGroups(RemoveHidden(tree));
    }

    public async Task<long> CreateAsync(SaveMenuRequest request, CancellationToken ct = default)
    {
        await EnsureValidParentAsync(request.ParentId, null, ct);
        var menu = new Menu
        {
            ParentId = request.ParentId,
            Title = request.Title,
            Icon = request.Icon,
            Path = request.Path,
            PermissionCode = request.PermissionCode,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            HiddenByDefault = request.HiddenByDefault,
        };
        db.Menus.Add(menu);
        await db.SaveChangesAsync(ct);
        return menu.Id;
    }

    public async Task UpdateAsync(long id, SaveMenuRequest request, CancellationToken ct = default)
    {
        var menu = await db.Menus.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException(nameof(Menu), id);
        await EnsureValidParentAsync(request.ParentId, id, ct);

        menu.ParentId = request.ParentId;
        menu.Title = request.Title;
        menu.Icon = request.Icon;
        menu.Path = request.Path;
        menu.PermissionCode = request.PermissionCode;
        menu.SortOrder = request.SortOrder;
        menu.IsActive = request.IsActive;
        menu.HiddenByDefault = request.HiddenByDefault;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var menu = await db.Menus.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException(nameof(Menu), id);
        if (await db.Menus.AnyAsync(m => m.ParentId == id, ct))
        {
            throw new DomainException("Delete or move the child menus first.");
        }
        if (await db.MenuRoleLayouts.AnyAsync(l => l.MenuId == id || l.ParentId == id, ct))
        {
            throw new DomainException("This menu is used by a role's custom layout. Reset that role's layout first.");
        }
        db.Menus.Remove(menu);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Upserts (roleId, menuId)'s position override — this role's sidebar only, the shared Menu catalog is untouched.</summary>
    public async Task SetRoleMenuPositionAsync(
        long roleId, long menuId, SetMenuPositionRequest request, CancellationToken ct = default)
    {
        if (!await db.Roles.AnyAsync(r => r.Id == roleId, ct))
        {
            throw new NotFoundException(nameof(Role), roleId);
        }
        if (!await db.Menus.AnyAsync(m => m.Id == menuId, ct))
        {
            throw new NotFoundException(nameof(Menu), menuId);
        }
        if (request.ParentId is { } parentId)
        {
            if (parentId == menuId)
            {
                throw new DomainException("A menu cannot be its own parent.");
            }
            if (!await db.Menus.AnyAsync(m => m.Id == parentId, ct))
            {
                throw new DomainException("Parent menu does not exist.");
            }
            if (await IsDescendantForRoleAsync(roleId, menuId, parentId, ct))
            {
                throw new DomainException("This would create a menu loop.");
            }
        }

        var layout = await db.MenuRoleLayouts.FirstOrDefaultAsync(l => l.RoleId == roleId && l.MenuId == menuId, ct);
        if (layout is null)
        {
            layout = new MenuRoleLayout { RoleId = roleId, MenuId = menuId };
            db.MenuRoleLayouts.Add(layout);
        }
        layout.ParentId = request.ParentId;
        layout.SortOrder = request.SortOrder;
        layout.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        layout.Hidden = request.Hidden;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Drops (roleId, menuId)'s override — that item goes back to its default global position for this role.</summary>
    public async Task ResetRoleMenuPositionAsync(long roleId, long menuId, CancellationToken ct = default)
    {
        var layout = await db.MenuRoleLayouts.FirstOrDefaultAsync(l => l.RoleId == roleId && l.MenuId == menuId, ct);
        if (layout is not null)
        {
            db.MenuRoleLayouts.Remove(layout);
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task EnsureValidParentAsync(long? parentId, long? selfId, CancellationToken ct)
    {
        if (parentId is null)
        {
            return;
        }
        if (parentId == selfId)
        {
            throw new DomainException("A menu cannot be its own parent.");
        }
        if (!await db.Menus.AnyAsync(m => m.Id == parentId, ct))
        {
            throw new DomainException("Parent menu does not exist.");
        }
        if (selfId is not null && await IsDescendantAsync(selfId.Value, parentId.Value, ct))
        {
            throw new DomainException("This would create a menu loop.");
        }
    }

    /// <summary>Walks up from candidateAncestorId to root along the GLOBAL parent chain, checking whether it passes through nodeId.</summary>
    private async Task<bool> IsDescendantAsync(long nodeId, long candidateAncestorId, CancellationToken ct)
    {
        var current = await db.Menus.AsNoTracking()
            .Where(m => m.Id == candidateAncestorId)
            .Select(m => new { m.Id, m.ParentId })
            .FirstOrDefaultAsync(ct);

        while (current is not null)
        {
            if (current.Id == nodeId)
            {
                return true;
            }
            if (current.ParentId is null)
            {
                return false;
            }
            current = await db.Menus.AsNoTracking()
                .Where(m => m.Id == current.ParentId)
                .Select(m => new { m.Id, m.ParentId })
                .FirstOrDefaultAsync(ct);
        }
        return false;
    }

    /// <summary>
    /// Same ancestor walk as IsDescendantAsync, but along this role's EFFECTIVE parent chain —
    /// at each node, follows that role's override parent when one exists, else the global one.
    /// </summary>
    private async Task<bool> IsDescendantForRoleAsync(long roleId, long nodeId, long candidateAncestorId, CancellationToken ct)
    {
        long? currentId = candidateAncestorId;
        while (currentId is not null)
        {
            if (currentId == nodeId)
            {
                return true;
            }

            var layout = await db.MenuRoleLayouts.AsNoTracking()
                .FirstOrDefaultAsync(l => l.RoleId == roleId && l.MenuId == currentId, ct);
            if (layout is not null)
            {
                currentId = layout.ParentId;
                continue;
            }

            var menu = await db.Menus.AsNoTracking()
                .Where(m => m.Id == currentId)
                .Select(m => new { m.ParentId })
                .FirstOrDefaultAsync(ct);
            currentId = menu?.ParentId;
        }
        return false;
    }

    private static List<MenuNodeDto> BuildTree(
        List<Menu> menus,
        Func<Menu, (long? ParentId, int SortOrder, string? Title, bool Hidden, bool IsOverridden)>? resolvePosition = null)
    {
        var resolve = resolvePosition ?? (m => (m.ParentId, m.SortOrder, null, false, false));
        var resolved = menus.Select(m => (Menu: m, Position: resolve(m))).ToList();
        var byParent = resolved.ToLookup(x => x.Position.ParentId);

        List<MenuNodeDto> Build(long? parentId) =>
            byParent[parentId]
                .OrderBy(x => x.Position.SortOrder)
                .Select(x => new MenuNodeDto(x.Menu.Id, x.Position.ParentId, x.Position.Title ?? x.Menu.Title, x.Menu.Icon, x.Menu.Path,
                    x.Menu.PermissionCode, x.Position.SortOrder, x.Menu.IsActive, x.Position.IsOverridden, x.Position.Hidden,
                    x.Menu.HiddenByDefault, Build(x.Menu.Id)))
                .ToList();

        return Build(null);
    }

    /// <summary>Drops group nodes (no path) whose visible children were all filtered out.</summary>
    private static List<MenuNodeDto> PruneEmptyGroups(IReadOnlyList<MenuNodeDto> nodes) =>
        nodes
            .Select(n => n with { Children = PruneEmptyGroups(n.Children) })
            .Where(n => n.Path is not null || n.Children.Count > 0)
            .ToList();

    /// <summary>Drops a role-hidden node and its whole subtree — used only for the caller's own sidebar (GetMyMenuAsync); the admin "By menu" editor keeps hidden nodes visible (marked IsHidden) so they can be un-hidden.</summary>
    private static List<MenuNodeDto> RemoveHidden(IReadOnlyList<MenuNodeDto> nodes) =>
        nodes
            .Where(n => !n.IsHidden)
            .Select(n => n with { Children = RemoveHidden(n.Children) })
            .ToList();
}
