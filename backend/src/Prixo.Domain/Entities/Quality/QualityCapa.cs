using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Quality;

/// <summary>
/// CAPA tracker (plan §20: "action, owner, due, verification"). Polymorphic source — SourceType
/// + SourceId, no FK — same untyped-association idiom as BillItem.SourceType/SourceId, since a
/// CAPA can be raised against an Incident, a NonConformance, an Audit finding or a Risk.
/// </summary>
public class QualityCapa : BranchEntity
{
    public CapaSourceType SourceType { get; set; }
    public long SourceId { get; set; }

    public required string ActionPlan { get; set; }
    public long OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }
    public DateOnly DueDate { get; set; }

    public CapaStatus Status { get; set; } = CapaStatus.Open;
    public DateTimeOffset? CompletedAt { get; set; }

    public long? VerifiedByUserId { get; set; }
    public User? VerifiedByUser { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public string? VerificationNotes { get; set; }
}
