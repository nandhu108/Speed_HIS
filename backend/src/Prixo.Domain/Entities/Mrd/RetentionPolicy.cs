using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Mrd;

/// <summary>
/// Record Retention (plan §17: "policy per record type"). Hospital-wide (not branch-scoped),
/// same posture as other reference data — but kept in the `mrd` schema rather than `master`
/// since RetentionYears is domain-specific and the generic Code/Name master framework has no
/// slot for it.
/// </summary>
public class RetentionPolicy : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int RetentionYears { get; set; }
    public string? Description { get; set; }
}

/// <summary>Destruction Register (plan §17) — one row per record file flagged for destruction once past its retention policy's window.</summary>
public class DestructionRegisterEntry : BaseEntity
{
    public long RecordFileId { get; set; }
    public RecordFile? RecordFile { get; set; }
    public long RetentionPolicyId { get; set; }
    public RetentionPolicy? RetentionPolicy { get; set; }

    public DateOnly EligibleDate { get; set; }
    public DestructionStatus Status { get; set; } = DestructionStatus.Eligible;

    public long? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    public long? DestroyedByUserId { get; set; }
    public User? DestroyedByUser { get; set; }
    public DateTimeOffset? DestroyedAt { get; set; }
    public string? DestructionMethod { get; set; }
    public string? Notes { get; set; }
}
