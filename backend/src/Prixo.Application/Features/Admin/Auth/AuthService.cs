using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Admin.Auth;

public class AuthService(
    IAppDbContext db,
    IJwtTokenService tokens,
    IPasswordHasherService hasher,
    PasswordPolicyService passwordPolicy,
    PermissionService permissions,
    SettingService settings,
    IActivityLogger activity,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    // A valid PBKDF2 hash of a throwaway secret, computed once. When a login names an unknown
    // or inactive user we still run a verify against this so the request takes the same time
    // as a real account with a wrong password — otherwise the ~10× faster "no hash" path lets
    // an attacker enumerate valid usernames by timing alone. Lazily initialised (the hasher is
    // injected); a first-call race just computes it twice, harmlessly.
    private static string? _decoyHash;

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserBranches).ThenInclude(ub => ub.Branch)
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

        if (user is null || !user.IsActive)
        {
            // Equalise timing with the wrong-password path below (anti-enumeration).
            _decoyHash ??= hasher.Hash($"decoy-{Guid.NewGuid():N}");
            hasher.Verify(_decoyHash, request.Password);
            await activity.LogAsync(ActivityEvents.LoginFailed,
                $"Login failed for '{request.Username}': unknown or inactive user",
                username: request.Username, saveImmediately: true, ct: ct);
            throw new DomainException("Invalid username or password.");
        }

        if (user.LockedUntil is { } lockedUntil && lockedUntil > now)
        {
            var minutesLeft = Math.Ceiling((lockedUntil - now).TotalMinutes);
            throw new DomainException($"Account is locked. Try again in {minutesLeft} minute(s).");
        }

        if (!hasher.Verify(user.PasswordHash, request.Password))
        {
            user.FailedLoginCount++;
            var maxAttempts = await settings.GetIntAsync(SettingKeys.LockoutMaxFailedAttempts, 5, ct);
            if (maxAttempts > 0 && user.FailedLoginCount >= maxAttempts)
            {
                var lockMinutes = await settings.GetIntAsync(SettingKeys.LockoutMinutes, 15, ct);
                user.LockedUntil = now.AddMinutes(lockMinutes);
                user.FailedLoginCount = 0;
                await activity.LogAsync(ActivityEvents.AccountLocked,
                    $"Account '{user.Username}' locked for {lockMinutes} minutes after failed logins",
                    user.Id, user.Username, ct: ct);
            }
            await activity.LogAsync(ActivityEvents.LoginFailed,
                $"Login failed for '{user.Username}': wrong password", user.Id, user.Username, ct: ct);
            await db.SaveChangesAsync(ct);
            throw new DomainException("Invalid username or password.");
        }

        // Resolve branch: requested one (must be mapped) or the user's default/first.
        var branches = user.UserBranches.Where(ub => ub.Branch.IsActive).Select(ub => ub.Branch).ToList();
        var branch = request.BranchId is { } requestedBranchId
            ? branches.FirstOrDefault(b => b.Id == requestedBranchId)
                ?? throw new DomainException("You are not mapped to the selected branch.")
            : branches.FirstOrDefault(b => b.Id == user.DefaultBranchId) ?? branches.FirstOrDefault()
                ?? throw new DomainException("No branch is mapped to this account. Contact the administrator.");

        var maxConcurrent = await settings.GetIntAsync(SettingKeys.SessionMaxConcurrent, 0, ct);
        if (maxConcurrent > 0)
        {
            var activeSessions = await db.UserSessions
                .CountAsync(s => s.UserId == user.Id && !s.IsRevoked && s.LoggedOutAt == null, ct);
            if (activeSessions >= maxConcurrent)
            {
                throw new DomainException($"Maximum concurrent sessions ({maxConcurrent}) reached. Log out elsewhere first.");
            }
        }

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = now;

        var sessionId = Guid.NewGuid();
        db.UserSessions.Add(new UserSession
        {
            UserId = user.Id,
            SessionId = sessionId,
            BranchId = branch.Id,
            IpAddress = currentUser.IpAddress,
            UserAgent = currentUser.UserAgent,
            LoginAt = now,
            LastActivityAt = now,
        });

        var (refreshToken, refreshHash) = tokens.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            SessionId = sessionId,
            TokenHash = refreshHash,
            ExpiresAt = now.AddDays(tokens.RefreshTokenDays),
        });

        await activity.LogAsync(ActivityEvents.Login,
            $"'{user.Username}' logged in to branch '{branch.Name}'", user.Id, user.Username, ct: ct);
        await db.SaveChangesAsync(ct);

        return await BuildLoginResponseAsync(user, branch, sessionId, refreshToken, ct);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        var incomingHash = tokens.HashRefreshToken(request.RefreshToken);

        var stored = await db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(t => t.User).ThenInclude(u => u.UserBranches).ThenInclude(ub => ub.Branch)
            .FirstOrDefaultAsync(t => t.TokenHash == incomingHash, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (stored.RevokedAt is not null)
        {
            // Reuse of a rotated token: assume theft, kill the whole session.
            await RevokeSessionInternalAsync(stored.SessionId, "refresh token reuse detected", ct);
            await activity.LogAsync(ActivityEvents.TokenReuseDetected,
                $"Refresh token reuse for user '{stored.User.Username}' — session revoked",
                stored.UserId, stored.User.Username, ct: ct);
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Session revoked due to token reuse.");
        }

        if (!stored.IsUsable(now))
        {
            throw new UnauthorizedAccessException("Refresh token expired.");
        }

        var session = await db.UserSessions
            .FirstOrDefaultAsync(s => s.SessionId == stored.SessionId, ct);
        if (session is null || session.IsRevoked || session.LoggedOutAt is not null || !stored.User.IsActive)
        {
            throw new UnauthorizedAccessException("Session is no longer valid.");
        }

        var branch = stored.User.UserBranches.Select(ub => ub.Branch)
            .FirstOrDefault(b => b.Id == session.BranchId)
            ?? throw new UnauthorizedAccessException("Branch mapping removed.");

        // Rotate.
        var (newToken, newHash) = tokens.CreateRefreshToken();
        stored.RevokedAt = now;
        stored.ReplacedByTokenHash = newHash;
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = stored.UserId,
            SessionId = stored.SessionId,
            TokenHash = newHash,
            ExpiresAt = now.AddDays(tokens.RefreshTokenDays),
        });
        session.LastActivityAt = now;

        await db.SaveChangesAsync(ct);
        return await BuildLoginResponseAsync(stored.User, branch, stored.SessionId, newToken, ct);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        if (currentUser.SessionId is not { } sessionId)
        {
            return;
        }

        await RevokeSessionInternalAsync(sessionId, "logout", ct);
        await activity.LogAsync(ActivityEvents.Logout,
            $"'{currentUser.UserName}' logged out", currentUser.UserId, currentUser.UserName, ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<AuthUserDto> GetMeAsync(CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new UnauthorizedAccessException();

        var branchId = currentUser.BranchId ?? user.DefaultBranchId
            ?? throw new UnauthorizedAccessException();
        var branchInfo = await db.Branches.AsNoTracking()
            .Where(b => b.Id == branchId)
            .Select(b => new { b.Name, b.DefaultLandingPath })
            .FirstAsync(ct);

        return await BuildUserDtoAsync(user, branchId, branchInfo.Name, branchInfo.DefaultLandingPath, ct);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new UnauthorizedAccessException();

        if (!hasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            throw new DomainException("Current password is incorrect.");
        }

        await passwordPolicy.ValidateAsync(request.NewPassword, ct);
        await passwordPolicy.EnsureNotReusedAsync(user.Id, request.NewPassword, ct);

        db.PasswordHistories.Add(new PasswordHistory { UserId = user.Id, PasswordHash = user.PasswordHash });
        user.PasswordHash = hasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.PasswordChangedAt = timeProvider.GetUtcNow();

        // Kill every other session so a stolen password stops working immediately.
        var otherSessions = await db.UserSessions
            .Where(s => s.UserId == user.Id && !s.IsRevoked && s.LoggedOutAt == null
                        && s.SessionId != currentUser.SessionId)
            .ToListAsync(ct);
        foreach (var session in otherSessions)
        {
            await RevokeSessionInternalAsync(session.SessionId, "password changed", ct);
        }

        await activity.LogAsync(ActivityEvents.PasswordChanged,
            $"'{user.Username}' changed their password", user.Id, user.Username, ct: ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task RevokeSessionInternalAsync(Guid sessionId, string reason, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();

        var session = await db.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);
        if (session is not null)
        {
            session.IsRevoked = true;
            session.LoggedOutAt = now;
        }

        var activeTokens = await db.RefreshTokens
            .Where(t => t.SessionId == sessionId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
            token.ReplacedByTokenHash = $"revoked: {reason}";
        }
    }

    private async Task<LoginResponse> BuildLoginResponseAsync(
        User user, Branch branch, Guid sessionId, string refreshToken, CancellationToken ct)
    {
        var roleCodes = user.UserRoles.Where(ur => ur.Role.IsActive).Select(ur => ur.Role.Code).ToList();
        var access = tokens.CreateAccessToken(user.Id, user.Username, user.FullName, branch.Id, sessionId, roleCodes);
        var userDto = await BuildUserDtoAsync(user, branch.Id, branch.Name, branch.DefaultLandingPath, ct);
        return new LoginResponse(access.Token, access.ExpiresAt, refreshToken, userDto);
    }

    private async Task<AuthUserDto> BuildUserDtoAsync(
        User user, long branchId, string branchName, string? branchDefaultLandingPath, CancellationToken ct)
    {
        permissions.Invalidate(user.Id);
        var permissionSet = await permissions.GetUserPermissionsAsync(user.Id, ct);
        var roleCodes = user.UserRoles.Where(ur => ur.Role.IsActive).Select(ur => ur.Role.Code).ToList();

        // A role's own DefaultLandingPath wins over the branch's; earliest-assigned role
        // wins on a conflict between two roles that both set one (deterministic, not that
        // most users will ever hold two roles with different overrides).
        var roleLandingPath = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .OrderBy(ur => ur.RoleId)
            .Select(ur => ur.Role.DefaultLandingPath)
            .FirstOrDefault(path => path is not null);
        var resolvedLandingPath = roleLandingPath ?? branchDefaultLandingPath;

        return new AuthUserDto(
            user.Id, user.Username, user.FullName, user.Email, user.PhotoPath,
            branchId, branchName, user.MustChangePassword,
            roleCodes, permissionSet.Order().ToList(), resolvedLandingPath);
    }
}
