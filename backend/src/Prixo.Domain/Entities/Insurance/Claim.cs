using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Insurance;

/// <summary>
/// A claim submitted to an insurer/TPA against a finalized Bill — the plan's "claim →
/// settlement → denial tracking" stages. Settlement fields are folded directly onto this
/// row (rather than split into a separate settlement entity, the way Billing/Finance split
/// Bill↔Receivable) since Claim is already owned by this one module.
/// </summary>
public class Claim : BranchEntity
{
    public required string ClaimNumber { get; set; }

    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long PolicyId { get; set; }
    public PatientInsurancePolicy? Policy { get; set; }
    /// <summary>Nullable — reimbursement-style claims can be submitted without a prior pre-auth.</summary>
    public long? PreAuthRequestId { get; set; }
    public PreAuthRequest? PreAuthRequest { get; set; }
    public long BillId { get; set; }
    public Bill? Bill { get; set; }

    public decimal ClaimAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Draft;

    public long? SubmittedByUserId { get; set; }
    public User? SubmittedByUser { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public long? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }

    public long? DenialReasonId { get; set; }
    public ClaimDenialReason? DenialReason { get; set; }
    public string? DenialNotes { get; set; }

    public decimal? SettledAmount { get; set; }
    public decimal? DeductionAmount { get; set; }
    public string? SettlementReferenceNumber { get; set; }
    public DateTimeOffset? SettledAt { get; set; }

    public ICollection<ClaimDocument> Documents { get; set; } = [];
}
