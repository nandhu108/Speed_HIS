namespace Prixo.Application.Common.Interfaces;

/// <summary>Refreshes the `reporting` schema's materialized views — the one place in the codebase that needs raw SQL execution, so it's kept behind its own small interface rather than widening IAppDbContext.</summary>
public interface IReportViewRefresher
{
    Task RefreshAllAsync(CancellationToken ct = default);
}
