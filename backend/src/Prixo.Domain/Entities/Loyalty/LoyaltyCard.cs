using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Loyalty;

/// <summary>
/// A loyalty membership card, issued to a Patient (the primary member). No separate
/// renewal-history table — the standard row-diff audit log (SaveChangesInterceptor)
/// already captures every field change on Renew/Upgrade/Deactivate.
/// </summary>
public class LoyaltyCard : BranchEntity
{
    public required string CardNumber { get; set; }
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long LoyaltyCardTypeId { get; set; }
    public LoyaltyCardType? LoyaltyCardType { get; set; }

    public DateOnly IssuedDate { get; set; }
    public int DurationMonths { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public decimal Amount { get; set; }

    public LoyaltyCardStatus Status { get; set; } = LoyaltyCardStatus.Active;
    public string? DeactivateReason { get; set; }
    public DateTimeOffset? DeactivatedAt { get; set; }
    public int DuplicateIssueCount { get; set; }
}

/// <summary>
/// A dependent linked to a card — an open-row-history join to a real Patient record,
/// mirroring AdmissionConsultant (AddedAt/RemovedAt instead of a separate transfer log).
/// </summary>
public class LoyaltyCardMember : BaseEntity
{
    public long LoyaltyCardId { get; set; }
    public LoyaltyCard? LoyaltyCard { get; set; }
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long MemberRelationTagId { get; set; }
    public MemberRelationTag? MemberRelationTag { get; set; }

    public DateTimeOffset AddedAt { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
}
