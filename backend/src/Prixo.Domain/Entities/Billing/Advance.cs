using Prixo.Domain.Common;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Patients;

namespace Prixo.Domain.Entities.Billing;

/// <summary>
/// A deposit on a patient's account — collected ahead of a procedure/admission and drawn
/// down against future bills. AdmissionId is nullable and optional: OP deposits (e.g. ahead
/// of a planned procedure) still have none; IPD admission deposits (Phase 9) set it, which
/// is what the admission's "Initial Deposit" step uses.
/// </summary>
public class Advance : BranchEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long? AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    public long PaymentModeId { get; set; }
    public PaymentMode? PaymentMode { get; set; }

    public decimal Amount { get; set; }
    /// <summary>Remaining, unapplied portion — decreases as the advance is drawn down against bills.</summary>
    public decimal BalanceAmount { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}
