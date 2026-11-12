using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Admin.Auth;

public class PasswordPolicyService(IAppDbContext db, SettingService settings, IPasswordHasherService hasher)
{
    /// <summary>Throws DomainException listing every violated rule.</summary>
    public async Task ValidateAsync(string password, CancellationToken ct = default)
    {
        var violations = new List<string>();

        var minLength = await settings.GetIntAsync(SettingKeys.PasswordMinLength, 8, ct);
        if (password.Length < minLength)
        {
            violations.Add($"at least {minLength} characters");
        }
        if (await settings.GetBoolAsync(SettingKeys.PasswordRequireUppercase, true, ct) && !password.Any(char.IsUpper))
        {
            violations.Add("an uppercase letter");
        }
        if (await settings.GetBoolAsync(SettingKeys.PasswordRequireLowercase, true, ct) && !password.Any(char.IsLower))
        {
            violations.Add("a lowercase letter");
        }
        if (await settings.GetBoolAsync(SettingKeys.PasswordRequireDigit, true, ct) && !password.Any(char.IsDigit))
        {
            violations.Add("a digit");
        }
        if (await settings.GetBoolAsync(SettingKeys.PasswordRequireSpecial, true, ct) && password.All(char.IsLetterOrDigit))
        {
            violations.Add("a special character");
        }

        if (violations.Count > 0)
        {
            throw new DomainException($"Password must contain {string.Join(", ", violations)}.");
        }
    }

    /// <summary>
    /// Whether accounts should be flagged "must change password at first login". Settings-driven
    /// like every other password rule here, so an environment that doesn't want the forced change
    /// can turn it off from Admin › Settings instead of needing a code change. Defaults to true —
    /// the secure posture, and what a real deployment keeps.
    /// </summary>
    public Task<bool> IsForceChangeOnFirstLoginEnabledAsync(CancellationToken ct = default) =>
        settings.GetBoolAsync(SettingKeys.PasswordForceChangeOnFirstLogin, true, ct);

    public async Task EnsureNotReusedAsync(long userId, string newPassword, CancellationToken ct = default)
    {
        var historyCount = await settings.GetIntAsync(SettingKeys.PasswordHistoryCount, 5, ct);
        if (historyCount <= 0)
        {
            return;
        }

        var recentHashes = await db.PasswordHistories.AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(historyCount)
            .Select(h => h.PasswordHash)
            .ToListAsync(ct);

        if (recentHashes.Any(hash => hasher.Verify(hash, newPassword)))
        {
            throw new DomainException($"Password must differ from your last {historyCount} passwords.");
        }
    }
}
