using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Biomed;

/// <summary>Preventive Maintenance schedule template (plan §19: "schedule templates → auto work orders"). "Generate due work orders" is an explicit action (WorkOrderService.GenerateDueAsync), not a background job — same honest scoping call Billing made for bed-charge refresh.</summary>
public class PmSchedule : BranchEntity
{
    public long EquipmentId { get; set; }
    public BiomedEquipment? Equipment { get; set; }

    public int FrequencyDays { get; set; }
    public DateOnly NextDueDate { get; set; }
    public DateOnly? LastGeneratedDate { get; set; }
    public string? ChecklistNotes { get; set; }
}
