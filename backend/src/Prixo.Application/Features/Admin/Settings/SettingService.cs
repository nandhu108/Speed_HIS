using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;

namespace Prixo.Application.Features.Admin.Settings;

public record SettingDto(long Id, string Key, string Value, string? Description, string Group);
public record UpdateSettingRequest(string Value);

public class SettingService(IAppDbContext db, IMemoryCache cache, IActivityLogger activity)
{
    private const string CachePrefix = "setting:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<SettingDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.SystemSettings.AsNoTracking()
            .OrderBy(s => s.Group).ThenBy(s => s.Key)
            .Select(s => new SettingDto(s.Id, s.Key, s.Value, s.Description, s.Group))
            .ToListAsync(ct);

    public async Task UpdateAsync(string key, UpdateSettingRequest request, CancellationToken ct = default)
    {
        var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Admin.SystemSetting), key);

        var oldValue = setting.Value;
        setting.Value = request.Value;
        await activity.LogAsync(ActivityEvents.SettingChanged, $"Setting '{key}' changed from '{oldValue}' to '{request.Value}'", ct: ct);
        await db.SaveChangesAsync(ct);
        cache.Remove(CachePrefix + key);
    }

    public async Task<int> GetIntAsync(string key, int fallback, CancellationToken ct = default)
    {
        var raw = await GetRawAsync(key, ct);
        return int.TryParse(raw, out var value) ? value : fallback;
    }

    public async Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken ct = default)
    {
        var raw = await GetRawAsync(key, ct);
        return bool.TryParse(raw, out var value) ? value : fallback;
    }

    public async Task<decimal> GetDecimalAsync(string key, decimal fallback, CancellationToken ct = default)
    {
        var raw = await GetRawAsync(key, ct);
        return decimal.TryParse(raw, out var value) ? value : fallback;
    }

    public async Task<string> GetStringAsync(string key, string fallback, CancellationToken ct = default) =>
        await GetRawAsync(key, ct) is { Length: > 0 } value ? value : fallback;

    public async Task<string?> GetRawAsync(string key, CancellationToken ct)
    {
        if (cache.TryGetValue(CachePrefix + key, out string? cached))
        {
            return cached;
        }

        var value = await db.SystemSettings.AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

        cache.Set(CachePrefix + key, value, CacheTtl);
        return value;
    }
}
