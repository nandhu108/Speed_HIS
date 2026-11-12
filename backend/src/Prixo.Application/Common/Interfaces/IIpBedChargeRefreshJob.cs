namespace Prixo.Application.Common.Interfaces;

/// <summary>Recurring Hangfire job: refreshes every open IP bill's bed-charge lines daily and auto-sweeps admission deposits against them.</summary>
public interface IIpBedChargeRefreshJob
{
    Task RunAsync(CancellationToken ct = default);
}
