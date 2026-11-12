namespace Prixo.Application.Common.Interfaces;

/// <summary>Recurring Hangfire job: refreshes the `reporting` schema's materialized views.</summary>
public interface IReportRefreshJob
{
    Task RunAsync(CancellationToken ct = default);
}
