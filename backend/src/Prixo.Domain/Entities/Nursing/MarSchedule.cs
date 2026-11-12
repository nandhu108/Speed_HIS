using Prixo.Domain.Common;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Nursing;

/// <summary>
/// One due-time row generated from a MedicationOrder's Frequency (NursingMath.GenerateMarSchedule).
/// IsHighAlert snapshots Drug.IsHighAlert at generation time so a later master edit doesn't
/// silently change an in-progress schedule's double-verification requirement.
/// </summary>
public class MarSchedule : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public long MedicationOrderId { get; set; }
    public MedicationOrder? MedicationOrder { get; set; }

    public DateTimeOffset DueAt { get; set; }
    public MarStatus Status { get; set; } = MarStatus.Pending;
    public bool IsHighAlert { get; set; }

    public MarAdministration? Administration { get; set; }
}
