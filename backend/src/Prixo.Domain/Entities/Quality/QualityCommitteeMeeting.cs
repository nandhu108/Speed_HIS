using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;

namespace Prixo.Domain.Entities.Quality;

/// <summary>Committee Meeting Records (plan §20: "minutes, action items"). Action items are a shallow child list — deliberately not routed through the CAPA tracker, which the plan lists as a separate bullet.</summary>
public class QualityCommitteeMeeting : BranchEntity
{
    public long CommitteeId { get; set; }
    public QualityCommittee? Committee { get; set; }

    public DateOnly MeetingDate { get; set; }
    public required string Minutes { get; set; }
    public string? AttendeesText { get; set; }

    public List<QualityCommitteeActionItem> ActionItems { get; set; } = [];
}

public class QualityCommitteeActionItem : BaseEntity
{
    public long MeetingId { get; set; }
    public QualityCommitteeMeeting? Meeting { get; set; }

    public required string Description { get; set; }
    public long OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }
    public DateOnly DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
