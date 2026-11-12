namespace Prixo.Application.Features.Billing;

public record AdvanceDto(
    long Id, long PatientId, string PatientUhid, string PatientName, long? AdmissionId, decimal Amount,
    decimal BalanceAmount, long PaymentModeId, string PaymentModeName, string? ReferenceNumber, string? Notes,
    DateTimeOffset ReceivedAt);

public record CollectAdvanceRequest(
    long PatientId, long? AdmissionId, decimal Amount, long PaymentModeId, string? ReferenceNumber, string? Notes);

/// <summary>
/// Moves part or all of an advance's unused BalanceAmount to a different patient and/or a
/// different admission — a correction for a deposit collected against the wrong record, not a
/// draw-down. Reduces the source Advance in place and creates a brand-new Advance row for the
/// target (same PaymentModeId/ReferenceNumber, since it's the same money just reassigned) —
/// append-only, so the source's original history stays intact rather than being overwritten.
/// TargetPatientId/TargetAdmissionId may both change in one transfer.
/// </summary>
public record TransferAdvanceRequest(long TargetPatientId, long? TargetAdmissionId, decimal Amount, string? Notes);
