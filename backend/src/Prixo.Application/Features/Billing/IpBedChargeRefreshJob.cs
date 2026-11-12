using Prixo.Application.Common.Interfaces;

namespace Prixo.Application.Features.Billing;

/// <summary>
/// Daily Hangfire job closing the previously-documented gap "no daily Hangfire job
/// auto-refreshing bed charges — manual only" (plan §6). Delegates to
/// BillingService.RefreshAllOpenIpBedChargesAsync, which also auto-sweeps admission deposits
/// against each recomputed balance. Runs with no HTTP context, same posture as NoShowJob.
/// </summary>
public class IpBedChargeRefreshJob(BillingService billing) : IIpBedChargeRefreshJob
{
    public async Task RunAsync(CancellationToken ct = default) => await billing.RefreshAllOpenIpBedChargesAsync(ct);
}
