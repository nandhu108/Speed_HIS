using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Infrastructure.Persistence;

namespace Prixo.Infrastructure.Services;

/// <summary>The one place in the codebase that runs raw SQL — REFRESH MATERIALIZED VIEW isn't expressible through EF Core's LINQ surface, so this needs the concrete AppDbContext rather than IAppDbContext. Each call is a literal string (no interpolation/concatenation) so there's no dynamic-SQL surface to sanitize.</summary>
public class ReportViewRefresher(AppDbContext db) : IReportViewRefresher
{
    public async Task RefreshAllAsync(CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW CONCURRENTLY reporting.mv_revenue_daily", ct);
        await db.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW CONCURRENTLY reporting.mv_daily_collection", ct);
        await db.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW CONCURRENTLY reporting.mv_op_census", ct);
    }
}
