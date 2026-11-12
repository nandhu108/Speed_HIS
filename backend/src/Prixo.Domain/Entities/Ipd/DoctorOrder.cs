using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Ipd;

/// <summary>
/// Unified order-sheet header (plan §9) — polymorphic like OPD's ClinicalOrder. Medication
/// and Diet orders get a structured child row; Lab/Radiology/Nursing stay free-text on
/// Description (no test master/kitchen workflow wired up yet — same loose-coupling posture
/// used everywhere else in this codebase).
/// </summary>
public class DoctorOrder : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public DoctorOrderType OrderType { get; set; }
    public long OrderedByDoctorId { get; set; }
    public Doctor? OrderedByDoctor { get; set; }
    public DateTimeOffset OrderedAt { get; set; }
    public ClinicalOrderPriority Priority { get; set; } = ClinicalOrderPriority.Routine;
    public ClinicalOrderStatus Status { get; set; } = ClinicalOrderStatus.Ordered;

    public required string Description { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    public MedicationOrder? MedicationOrder { get; set; }
    public DietOrder? DietOrder { get; set; }
}

/// <summary>
/// Structured medication intent — schema-ready for Phase 10's MAR (due-time schedule
/// generation reads Frequency/StartDate/EndDate) but nothing generates a MAR schedule or a
/// Pharmacy Issue from this yet; both stay manual until those phases land.
/// </summary>
public class MedicationOrder : BaseEntity
{
    public long DoctorOrderId { get; set; }
    public DoctorOrder? DoctorOrder { get; set; }

    public long DrugId { get; set; }
    public Drug? Drug { get; set; }
    public required string Dose { get; set; }
    public required string Route { get; set; }
    public required string Frequency { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Instructions { get; set; }
}

/// <summary>Placeholder per plan §9 ("diet placeholder") — structured intent capture only, no diet-item master or kitchen worklist.</summary>
public class DietOrder : BaseEntity
{
    public long DoctorOrderId { get; set; }
    public DoctorOrder? DoctorOrder { get; set; }

    public required string DietType { get; set; }
    public string? Instructions { get; set; }
}
