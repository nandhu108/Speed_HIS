using Prixo.Application.Common.Interfaces;

namespace Prixo.Application.Features.Reports;

/// <summary>Hangfire recurring job (plan §23: "Reporting schema + materialized views + refresh jobs") — delegates the actual `REFRESH MATERIALIZED VIEW` SQL to Infrastructure's IReportViewRefresher, same split as every other Application-layer job (see NoShowJob) that needs a persistence-specific primitive.</summary>
public class ReportRefreshJob(IReportViewRefresher refresher) : IReportRefreshJob
{
    public Task RunAsync(CancellationToken ct = default) => refresher.RefreshAllAsync(ct);
}
