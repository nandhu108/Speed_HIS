namespace Prixo.Application.Common.Interfaces;

/// <summary>
/// Lets an earlier-phase module (OPD) collect a patient advance without a hard dependency
/// on Billing's concrete AdvanceService — same posture as IFinanceLedgerPoster.
/// </summary>
public interface IAdvanceCollector
{
    Task<long> CollectAsync(
        long patientId, long? admissionId, decimal amount, long paymentModeId, string? referenceNumber,
        CancellationToken ct = default);
}
