using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Infrastructure.Persistence;

namespace Prixo.Infrastructure.Services;

/// <summary>
/// Atomic, gap-tolerant counters backed by a single upsert statement — safe under
/// concurrent requests without application-level locking.
/// </summary>
public class SequenceService(AppDbContext db) : ISequenceService
{
    private const string UpsertSql = """
        INSERT INTO admin.sequence_counters (sequence_key, scope_id, year, last_value)
        VALUES ({0}, {1}, {2}, 1)
        ON CONFLICT (sequence_key, scope_id, year)
        DO UPDATE SET last_value = admin.sequence_counters.last_value + 1
        RETURNING last_value
        """;

    public async Task<long> NextValueAsync(string sequenceKey, long scopeId, int year, CancellationToken ct = default)
    {
        var results = await db.Database
            .SqlQueryRaw<long>(UpsertSql, sequenceKey, scopeId, year)
            .ToListAsync(ct);
        return results[0];
    }
}
