using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Admin.Auth;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Admin.Users;

public class UserQuery : PagedQuery
{
    public long? DepartmentId { get; set; }
    public long? DesignationId { get; set; }
    public long? RoleId { get; set; }
    public long? BranchId { get; set; }
    public bool? IsActive { get; set; }
}

public class UserService(
    IAppDbContext db,
    IPasswordHasherService hasher,
    PasswordPolicyService passwordPolicy,
    PermissionService permissions,
    IActivityLogger activity,
    TimeProvider timeProvider)
{
    public async Task<PagedResult<UserListItemDto>> GetPagedAsync(UserQuery query, CancellationToken ct = default)
    {
        var users = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            users = users.Where(u =>
                EF.Functions.ILike(u.Username, term) ||
                EF.Functions.ILike(u.FullName, term) ||
                (u.Email != null && EF.Functions.ILike(u.Email, term)) ||
                (u.EmployeeCode != null && EF.Functions.ILike(u.EmployeeCode, term)));
        }
        if (query.DepartmentId.HasValue)
        {
            users = users.Where(u => u.DepartmentId == query.DepartmentId);
        }
        if (query.DesignationId.HasValue)
        {
            users = users.Where(u => u.DesignationId == query.DesignationId);
        }
        if (query.RoleId.HasValue)
        {
            users = users.Where(u => u.UserRoles.Any(ur => ur.RoleId == query.RoleId));
        }
        if (query.BranchId.HasValue)
        {
            users = users.Where(u => u.UserBranches.Any(ub => ub.BranchId == query.BranchId));
        }
        if (query.IsActive.HasValue)
        {
            users = users.Where(u => u.IsActive == query.IsActive);
        }

        var totalCount = await users.CountAsync(ct);
        var now = timeProvider.GetUtcNow();

        var items = await users
            .OrderBy(u => u.FullName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new UserListItemDto(
                u.Id, u.Username, u.FullName, u.Email, u.Phone, u.EmployeeCode,
                u.Designation != null ? u.Designation.Name : null,
                u.Department != null ? u.Department.Name : null,
                u.IsActive, u.LockedUntil != null && u.LockedUntil > now, u.LastLoginAt,
                u.UserRoles.Select(ur => ur.Role.Name).ToList()))
            .ToListAsync(ct);

        return PagedResult<UserListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<UserDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking()
            .Include(u => u.UserRoles)
            .Include(u => u.UserBranches)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException(nameof(User), id);

        return new UserDetailDto(
            user.Id, user.Username, user.FullName, user.Email, user.Phone,
            user.EmployeeCode, user.DesignationId, user.DepartmentId, user.PhotoPath, user.DefaultBranchId,
            user.IsActive, user.MustChangePassword, user.LastLoginAt,
            user.UserRoles.Select(ur => ur.RoleId).ToList(),
            user.UserBranches.Select(ub => ub.BranchId).ToList());
    }

    public async Task<long> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(u => u.Username == request.Username, ct))
        {
            throw new DomainException($"Username '{request.Username}' is already taken.");
        }
        await passwordPolicy.ValidateAsync(request.Password, ct);
        await EnsureReferencesExistAsync(request.RoleIds, request.BranchIds, ct);

        var user = new User
        {
            Username = request.Username,
            PasswordHash = hasher.Hash(request.Password),
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            EmployeeCode = request.EmployeeCode,
            DesignationId = request.DesignationId,
            DepartmentId = request.DepartmentId,
            DefaultBranchId = request.DefaultBranchId ?? request.BranchIds.First(),
            MustChangePassword = await passwordPolicy.IsForceChangeOnFirstLoginEnabledAsync(ct),
            UserRoles = request.RoleIds.Distinct().Select(roleId => new UserRole { RoleId = roleId }).ToList(),
            UserBranches = request.BranchIds.Distinct().Select(branchId => new UserBranch { BranchId = branchId }).ToList(),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user.Id;
    }

    public async Task UpdateAsync(long id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .Include(u => u.UserBranches)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException(nameof(User), id);

        await EnsureReferencesExistAsync(request.RoleIds, request.BranchIds, ct);

        var rolesChanged =
            !user.UserRoles.Select(ur => ur.RoleId).Order().SequenceEqual(request.RoleIds.Distinct().Order());

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.EmployeeCode = request.EmployeeCode;
        user.DesignationId = request.DesignationId;
        user.DepartmentId = request.DepartmentId;
        user.DefaultBranchId = request.DefaultBranchId;
        user.IsActive = request.IsActive;

        SyncJoinRows(user.UserRoles, request.RoleIds, ur => ur.RoleId,
            roleId => new UserRole { UserId = id, RoleId = roleId }, db.UserRoles);
        SyncJoinRows(user.UserBranches, request.BranchIds, ub => ub.BranchId,
            branchId => new UserBranch { UserId = id, BranchId = branchId }, db.UserBranches);

        if (rolesChanged)
        {
            await activity.LogAsync(ActivityEvents.UserRolesChanged,
                $"Roles changed for user '{user.Username}'", user.Id, user.Username, ct: ct);
        }

        await db.SaveChangesAsync(ct);
        permissions.Invalidate(id);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException(nameof(User), id);
        if (user.Id == 1)
        {
            throw new DomainException("The system administrator account cannot be deleted.");
        }

        db.Users.Remove(user); // converted to soft delete by the interceptor
        await db.SaveChangesAsync(ct);
        permissions.Invalidate(id);
    }

    public async Task SetActiveAsync(long id, bool isActive, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException(nameof(User), id);
        if (user.Id == 1 && !isActive)
        {
            throw new DomainException("The system administrator account cannot be deactivated.");
        }
        if (user.IsActive == isActive)
        {
            return;
        }

        user.IsActive = isActive;
        await activity.LogAsync(isActive ? ActivityEvents.UserActivated : ActivityEvents.UserDeactivated,
            $"User '{user.Username}' {(isActive ? "activated" : "deactivated")}", user.Id, user.Username, ct: ct);
        await db.SaveChangesAsync(ct);
        permissions.Invalidate(id);
    }

    public async Task ResetPasswordAsync(long id, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException(nameof(User), id);

        await passwordPolicy.ValidateAsync(request.NewPassword, ct);

        db.PasswordHistories.Add(new PasswordHistory { UserId = user.Id, PasswordHash = user.PasswordHash });
        user.PasswordHash = hasher.Hash(request.NewPassword);
        // The caller can ask for the forced change, but the environment setting can veto it — so a
        // dev/demo environment with the setting off never lands anyone on the change-password
        // screen, whatever the Users screen happens to send.
        user.MustChangePassword = request.MustChangePassword
            && await passwordPolicy.IsForceChangeOnFirstLoginEnabledAsync(ct);
        user.PasswordChangedAt = timeProvider.GetUtcNow();
        user.FailedLoginCount = 0;
        user.LockedUntil = null;

        await activity.LogAsync(ActivityEvents.PasswordReset,
            $"Password reset for '{user.Username}' by an administrator", user.Id, user.Username, ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<UserPermissionsDto> GetPermissionOverridesAsync(long userId, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
        {
            throw new NotFoundException(nameof(User), userId);
        }

        var rolePermissionCodes = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId && ur.Role.IsActive)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(ct);

        var overrides = await db.UserPermissionOverrides.AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(o => new PermissionOverrideDto(o.Permission.Code, o.IsGranted))
            .OrderBy(o => o.PermissionCode)
            .ToListAsync(ct);

        return new UserPermissionsDto(rolePermissionCodes, overrides);
    }

    public async Task SetPermissionOverridesAsync(
        long userId, SetUserPermissionOverridesRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        var existing = await db.UserPermissionOverrides
            .Where(o => o.UserId == userId)
            .ToListAsync(ct);

        var codes = request.Overrides.Select(o => o.PermissionCode).Distinct().ToList();
        var permissionIdByCode = await db.Permissions
            .Where(p => codes.Contains(p.Code))
            .ToDictionaryAsync(p => p.Code, p => p.Id, ct);
        if (permissionIdByCode.Count != codes.Count)
        {
            throw new DomainException("One or more permission codes are unknown.");
        }

        var desired = request.Overrides
            .ToDictionary(o => permissionIdByCode[o.PermissionCode], o => o.IsGranted);

        foreach (var row in existing.Where(row => !desired.ContainsKey(row.PermissionId)))
        {
            db.UserPermissionOverrides.Remove(row);
        }
        foreach (var (permissionId, isGranted) in desired)
        {
            var row = existing.FirstOrDefault(row => row.PermissionId == permissionId);
            if (row is null)
            {
                db.UserPermissionOverrides.Add(new UserPermissionOverride
                {
                    UserId = userId, PermissionId = permissionId, IsGranted = isGranted,
                });
            }
            else if (row.IsGranted != isGranted)
            {
                row.IsGranted = isGranted;
            }
        }

        await activity.LogAsync(ActivityEvents.UserPermissionOverridesChanged,
            $"Permission overrides changed for user '{user.Username}' ({request.Overrides.Count} overrides)",
            user.Id, user.Username, ct: ct);
        await db.SaveChangesAsync(ct);
        permissions.Invalidate(userId);
    }

    private async Task EnsureReferencesExistAsync(
        IReadOnlyList<long> roleIds, IReadOnlyList<long> branchIds, CancellationToken ct)
    {
        var validRoles = await db.Roles.CountAsync(r => roleIds.Contains(r.Id), ct);
        if (validRoles != roleIds.Distinct().Count())
        {
            throw new DomainException("One or more roles do not exist.");
        }
        var validBranches = await db.Branches.CountAsync(b => branchIds.Contains(b.Id), ct);
        if (validBranches != branchIds.Distinct().Count())
        {
            throw new DomainException("One or more branches do not exist.");
        }
    }

    private static void SyncJoinRows<TJoin>(
        ICollection<TJoin> current, IReadOnlyList<long> desired,
        Func<TJoin, long> keyOf, Func<long, TJoin> create, DbSet<TJoin> set)
        where TJoin : class
    {
        var desiredSet = desired.Distinct().ToHashSet();
        foreach (var row in current.Where(row => !desiredSet.Contains(keyOf(row))).ToList())
        {
            set.Remove(row);
            current.Remove(row);
        }
        foreach (var key in desiredSet.Where(key => current.All(row => keyOf(row) != key)))
        {
            current.Add(create(key));
        }
    }
}
