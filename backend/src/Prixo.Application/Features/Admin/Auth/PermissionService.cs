using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prixo.Application.Common.Interfaces;

namespace Prixo.Application.Features.Admin.Auth;

/// <summary>
/// Effective permission set per user, cached briefly. Call Invalidate after any
/// role/permission mutation so changes take effect promptly.
/// </summary>
public class PermissionService(IAppDbContext db, IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);
    private static string Key(long userId) => $"perms:{userId}";

    public async Task<IReadOnlySet<string>> GetUserPermissionsAsync(long userId, CancellationToken ct = default)
    {
        if (cache.TryGetValue(Key(userId), out IReadOnlySet<string>? cached) && cached is not null)
        {
            return cached;
        }

        var codes = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId && ur.Role.IsActive)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct()
            .ToListAsync(ct);

        var set = codes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var overrides = await db.UserPermissionOverrides.AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(o => new { o.Permission.Code, o.IsGranted })
            .ToListAsync(ct);
        foreach (var o in overrides)
        {
            if (o.IsGranted) set.Add(o.Code);
            else set.Remove(o.Code);
        }

        var result = (IReadOnlySet<string>)set;
        cache.Set(Key(userId), result, CacheTtl);
        return result;
    }

    public async Task<bool> HasPermissionAsync(long userId, string permissionCode, CancellationToken ct = default) =>
        (await GetUserPermissionsAsync(userId, ct)).Contains(permissionCode);

    public void Invalidate(long userId) => cache.Remove(Key(userId));

    public async Task InvalidateRoleMembersAsync(long roleId, CancellationToken ct = default)
    {
        var userIds = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync(ct);
        foreach (var userId in userIds)
        {
            Invalidate(userId);
        }
    }
}
