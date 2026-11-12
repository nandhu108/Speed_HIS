using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Auth;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Admin.Roles;

public record RoleDto(long Id, string Code, string Name, string? Description, bool IsSystem,
    bool IsActive, int UserCount, int PermissionCount, string? DefaultLandingPath);
public record SaveRoleRequest(string Code, string Name, string? Description, string? DefaultLandingPath = null);
public record SetRolePermissionsRequest(IReadOnlyList<string> PermissionCodes);
public record PermissionDto(long Id, string Code, string Module, string Resource, string Action, string? Description);

public class SaveRoleRequestValidator : AbstractValidator<SaveRoleRequest>
{
    public SaveRoleRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Z0-9_]+$").WithMessage("Code must be UPPER_SNAKE_CASE.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DefaultLandingPath).MaximumLength(200);
    }
}

public class RoleService(IAppDbContext db, PermissionService permissions, IActivityLogger activity)
{
    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Code, r.Name, r.Description, r.IsSystem, r.IsActive,
                r.UserRoles.Count, r.RolePermissions.Count, r.DefaultLandingPath))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionCatalogueAsync(CancellationToken ct = default) =>
        await db.Permissions.AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Resource).ThenBy(p => p.Action)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Module, p.Resource, p.Action, p.Description))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetRolePermissionsAsync(long roleId, CancellationToken ct = default)
    {
        if (!await db.Roles.AnyAsync(r => r.Id == roleId, ct))
        {
            throw new NotFoundException(nameof(Role), roleId);
        }
        return await db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission.Code)
            .OrderBy(code => code)
            .ToListAsync(ct);
    }

    public async Task<long> CreateAsync(SaveRoleRequest request, CancellationToken ct = default)
    {
        if (await db.Roles.AnyAsync(r => r.Code == request.Code, ct))
        {
            throw new DomainException($"Role code '{request.Code}' already exists.");
        }
        var role = new Role
        {
            Code = request.Code, Name = request.Name, Description = request.Description,
            DefaultLandingPath = request.DefaultLandingPath,
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        return role.Id;
    }

    public async Task UpdateAsync(long id, SaveRoleRequest request, CancellationToken ct = default)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Role), id);
        if (role.IsSystem && role.Code != request.Code)
        {
            throw new DomainException("A system role's code cannot be changed (it's the seeding key) — its name, description and default landing page can be.");
        }
        if (await db.Roles.AnyAsync(r => r.Code == request.Code && r.Id != id, ct))
        {
            throw new DomainException($"Role code '{request.Code}' already exists.");
        }

        role.Code = request.Code;
        role.Name = request.Name;
        role.Description = request.Description;
        role.DefaultLandingPath = request.DefaultLandingPath;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var role = await db.Roles.Include(r => r.UserRoles).FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Role), id);
        if (role.IsSystem)
        {
            throw new DomainException("System roles cannot be deleted.");
        }
        if (role.UserRoles.Count > 0)
        {
            throw new DomainException("Role is assigned to users. Remove it from all users first.");
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetPermissionsAsync(long roleId, SetRolePermissionsRequest request, CancellationToken ct = default)
    {
        var role = await db.Roles.Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, ct)
            ?? throw new NotFoundException(nameof(Role), roleId);

        var desiredCodes = request.PermissionCodes.Distinct().ToList();
        var desired = await db.Permissions
            .Where(p => desiredCodes.Contains(p.Code))
            .Select(p => p.Id)
            .ToListAsync(ct);
        if (desired.Count != desiredCodes.Count)
        {
            throw new DomainException("One or more permission codes are unknown.");
        }

        ApplyPermissions(role, desired);
        await activity.LogAsync(ActivityEvents.RolePermissionsChanged,
            $"Permissions changed for role '{role.Name}' ({desiredCodes.Count} permissions)", ct: ct);
        await db.SaveChangesAsync(ct);
        await permissions.InvalidateRoleMembersAsync(roleId, ct);
    }

    /// <summary>
    /// Resets a system role's permissions back to exactly what DefaultRoleDefinitions says it
    /// should hold — undoes any manual drift from hand-tuning via SetPermissionsAsync. Only
    /// meaningful for system roles; a custom (non-system) role has no "default" to fall back to.
    /// </summary>
    public async Task RestoreDefaultPermissionsAsync(long roleId, CancellationToken ct = default)
    {
        var role = await db.Roles.Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, ct)
            ?? throw new NotFoundException(nameof(Role), roleId);
        if (!role.IsSystem)
        {
            throw new DomainException("Only system roles have a seeded default to restore.");
        }
        var definition = DefaultRoleDefinitions.Find(role.Code)
            ?? throw new DomainException($"No seeded default is registered for role code '{role.Code}'.");

        var desired = await db.Permissions
            .Where(p => definition.Permissions.Contains(p.Code))
            .Select(p => p.Id)
            .ToListAsync(ct);

        ApplyPermissions(role, desired);
        await activity.LogAsync(ActivityEvents.RolePermissionsChanged,
            $"Permissions restored to default for role '{role.Name}' ({desired.Count} permissions)", ct: ct);
        await db.SaveChangesAsync(ct);
        await permissions.InvalidateRoleMembersAsync(roleId, ct);
    }

    /// <summary>Diffs role.RolePermissions against the desired permission-id set and adds/removes rows to match — shared by SetPermissionsAsync and RestoreDefaultPermissionsAsync.</summary>
    private void ApplyPermissions(Role role, IReadOnlyCollection<long> desiredPermissionIds)
    {
        var desiredSet = desiredPermissionIds.ToHashSet();
        foreach (var row in role.RolePermissions.Where(rp => !desiredSet.Contains(rp.PermissionId)).ToList())
        {
            db.RolePermissions.Remove(row);
            role.RolePermissions.Remove(row);
        }
        foreach (var permissionId in desiredSet.Where(pid => role.RolePermissions.All(rp => rp.PermissionId != pid)))
        {
            role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
        }
    }
}
