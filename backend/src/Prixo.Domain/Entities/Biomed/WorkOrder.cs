using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Biomed;

/// <summary>The unit of maintenance work (plan §19) — raised either from a due PmSchedule or from a BreakdownComplaint. Completing one is what actually captures downtime (via the parent complaint) and closes the loop.</summary>
public class WorkOrder : BranchEntity
{
    public long EquipmentId { get; set; }
    public BiomedEquipment? Equipment { get; set; }
    public required string WorkOrderNumber { get; set; }
    public WorkOrderType WorkOrderType { get; set; }

    public long? PmScheduleId { get; set; }
    public PmSchedule? PmSchedule { get; set; }
    public long? BreakdownComplaintId { get; set; }
    public BreakdownComplaint? BreakdownComplaint { get; set; }

    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Open;
    public DateOnly ScheduledDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public long? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }
    public string? Notes { get; set; }

    public ICollection<SpareUsage> SpareUsages { get; set; } = [];
}
