using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Biomed;

/// <summary>Breakdown Complaint (plan §19: "raise → assign → resolve, downtime capture"). DowntimeMinutes is computed at ResolveAsync from ReportedAt→ResolvedAt, not user-entered.</summary>
public class BreakdownComplaint : BranchEntity
{
    public long EquipmentId { get; set; }
    public BiomedEquipment? Equipment { get; set; }
    public required string ComplaintNumber { get; set; }

    public long ReportedByUserId { get; set; }
    public User? ReportedByUser { get; set; }
    public DateTimeOffset ReportedAt { get; set; }
    public required string Description { get; set; }

    public ComplaintStatus Status { get; set; } = ComplaintStatus.Reported;
    public long? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }
    public DateTimeOffset? AssignedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public int? DowntimeMinutes { get; set; }
    public string? Resolution { get; set; }
}
