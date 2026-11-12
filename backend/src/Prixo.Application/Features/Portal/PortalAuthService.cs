using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Auth;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Portal;

/// <summary>
/// Patient Portal auth (plan §21: "password first, OTP when SMS integration lands"; "separate
/// JWT audience"). Mirrors Admin's AuthService shape (rotating hashed refresh tokens, reuse-
/// detection revokes the session) but simplified: no concurrent-session cap, no password-
/// history reuse check — both real gaps versus the staff auth, accepted for this phase's scope
/// (see module doc). RegisterAsync is the self-service "activate my portal account" flow:
/// identity is proven by knowing the UHID + mobile + date of birth already on file, not by an
/// admin provisioning an account.
/// </summary>
public class PortalAuthService(
    IAppDbContext db,
    IJwtTokenService tokens,
    IPasswordHasherService hasher,
    PasswordPolicyService passwordPolicy,
    IActivityLogger activity,
    IPortalCurrentUser currentUser,
    TimeProvider timeProvider)
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    public async Task RegisterAsync(RegisterPortalAccountRequest request, CancellationToken ct = default)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(
            p => p.Uhid == request.Uhid && p.Mobile == request.Mobile && p.DateOfBirth == request.DateOfBirth, ct)
            ?? throw new DomainException("No matching patient found for that UHID, mobile number and date of birth.");

        if (await db.PortalAccounts.AnyAsync(a => a.PatientId == patient.Id, ct))
        {
            throw new DomainException("A portal account already exists for this patient. Please log in instead.");
        }

        await passwordPolicy.ValidateAsync(request.Password, ct);

        db.PortalAccounts.Add(new PortalAccount { PatientId = patient.Id, PasswordHash = hasher.Hash(request.Password) });
        await activity.LogAsync(ActivityEvents.PortalRegistered, $"Portal account activated for patient {patient.Uhid}", username: patient.Uhid, ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PortalLoginResponse> LoginAsync(PortalLoginRequest request, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();

        var candidates = await db.Patients.AsNoTracking()
            .Where(p => p.Uhid == request.UhidOrMobile || p.Mobile == request.UhidOrMobile)
            .Select(p => p.Id)
            .ToListAsync(ct);
        if (candidates.Count > 1)
        {
            throw new DomainException("Multiple patients match that mobile number — please log in with your UHID instead.");
        }
        var patientId = candidates.FirstOrDefault();

        var account = patientId != 0
            ? await db.PortalAccounts.Include(a => a.Patient).FirstOrDefaultAsync(a => a.PatientId == patientId, ct)
            : null;
        if (account is null)
        {
            await activity.LogAsync(ActivityEvents.PortalLoginFailed, $"Portal login failed for '{request.UhidOrMobile}': no account", ct: ct, saveImmediately: true);
            throw new DomainException("Invalid credentials.");
        }

        if (account.LockedUntil is { } lockedUntil && lockedUntil > now)
        {
            var minutesLeft = Math.Ceiling((lockedUntil - now).TotalMinutes);
            throw new DomainException($"Account is locked. Try again in {minutesLeft} minute(s).");
        }

        if (!hasher.Verify(account.PasswordHash, request.Password))
        {
            account.FailedLoginAttempts++;
            if (account.FailedLoginAttempts >= MaxFailedAttempts)
            {
                account.LockedUntil = now.AddMinutes(LockoutMinutes);
                account.FailedLoginAttempts = 0;
            }
            await activity.LogAsync(ActivityEvents.PortalLoginFailed, $"Portal login failed for '{account.Patient!.Uhid}': wrong password", username: account.Patient.Uhid, ct: ct);
            await db.SaveChangesAsync(ct);
            throw new DomainException("Invalid credentials.");
        }

        account.FailedLoginAttempts = 0;
        account.LockedUntil = null;
        account.LastLoginAt = now;

        var sessionId = Guid.NewGuid();
        var (refreshToken, refreshHash) = tokens.CreateRefreshToken();
        db.PortalRefreshTokens.Add(new PortalRefreshToken
        {
            PatientId = account.PatientId, SessionId = sessionId, TokenHash = refreshHash,
            ExpiresAt = now.AddDays(tokens.PortalRefreshTokenDays),
        });

        await activity.LogAsync(ActivityEvents.PortalLogin, $"Patient '{account.Patient!.Uhid}' logged in to the portal", username: account.Patient.Uhid, ct: ct);
        await db.SaveChangesAsync(ct);

        return BuildResponse(account, sessionId, refreshToken);
    }

    public async Task<PortalLoginResponse> RefreshAsync(PortalRefreshRequest request, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        var incomingHash = tokens.HashRefreshToken(request.RefreshToken);

        var stored = await db.PortalRefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == incomingHash, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (stored.RevokedAt is not null)
        {
            await RevokeSessionInternalAsync(stored.SessionId, "refresh token reuse detected", ct);
            await activity.LogAsync(ActivityEvents.PortalTokenReuseDetected, "Portal refresh token reuse — session revoked", ct: ct);
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Session revoked due to token reuse.");
        }
        if (!stored.IsUsable(now))
        {
            throw new UnauthorizedAccessException("Refresh token expired.");
        }

        var account = await db.PortalAccounts.Include(a => a.Patient).FirstOrDefaultAsync(a => a.PatientId == stored.PatientId, ct)
            ?? throw new UnauthorizedAccessException("Account no longer valid.");

        var (newToken, newHash) = tokens.CreateRefreshToken();
        stored.RevokedAt = now;
        stored.ReplacedByTokenHash = newHash;
        db.PortalRefreshTokens.Add(new PortalRefreshToken
        {
            PatientId = stored.PatientId, SessionId = stored.SessionId, TokenHash = newHash,
            ExpiresAt = now.AddDays(tokens.PortalRefreshTokenDays),
        });
        await db.SaveChangesAsync(ct);

        return BuildResponse(account, stored.SessionId, newToken);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        if (currentUser.SessionId is not { } sessionId)
        {
            return;
        }
        await RevokeSessionInternalAsync(sessionId, "logout", ct);
        await activity.LogAsync(ActivityEvents.PortalLogout, "Patient logged out of the portal", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(PortalChangePasswordRequest request, CancellationToken ct = default)
    {
        var account = await db.PortalAccounts.Include(a => a.Patient).FirstOrDefaultAsync(a => a.PatientId == currentUser.PatientId, ct)
            ?? throw new UnauthorizedAccessException();

        if (!hasher.Verify(account.PasswordHash, request.CurrentPassword))
        {
            throw new DomainException("Current password is incorrect.");
        }
        await passwordPolicy.ValidateAsync(request.NewPassword, ct);

        account.PasswordHash = hasher.Hash(request.NewPassword);
        account.MustChangePassword = false;

        var otherTokens = await db.PortalRefreshTokens
            .Where(t => t.PatientId == account.PatientId && t.RevokedAt == null && t.SessionId != currentUser.SessionId)
            .ToListAsync(ct);
        var now = timeProvider.GetUtcNow();
        foreach (var token in otherTokens)
        {
            token.RevokedAt = now;
            token.ReplacedByTokenHash = "revoked: password changed";
        }

        await activity.LogAsync(ActivityEvents.PortalPasswordChanged, $"Patient '{account.Patient!.Uhid}' changed their portal password", username: account.Patient.Uhid, ct: ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task RevokeSessionInternalAsync(Guid sessionId, string reason, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var activeTokens = await db.PortalRefreshTokens.Where(t => t.SessionId == sessionId && t.RevokedAt == null).ToListAsync(ct);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
            token.ReplacedByTokenHash = $"revoked: {reason}";
        }
    }

    private PortalLoginResponse BuildResponse(PortalAccount account, Guid sessionId, string refreshToken)
    {
        var patient = account.Patient!;
        var access = tokens.CreatePortalAccessToken(patient.Id, patient.Uhid, $"{patient.FirstName} {patient.LastName}", sessionId);
        var dto = new PortalPatientDto(patient.Id, patient.Uhid, $"{patient.FirstName} {patient.LastName}", patient.Mobile, account.MustChangePassword);
        return new PortalLoginResponse(access.Token, access.ExpiresAt, refreshToken, dto);
    }
}
