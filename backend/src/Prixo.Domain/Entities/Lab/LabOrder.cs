using Prixo.Domain.Common;
using Prixo.Domain.Entities.Er;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Lab;

/// <summary>
/// Lab order header. Created independently by lab reception (from an OP visit, a walk-in,
/// or with a loose ClinicalOrderId back-reference for traceability) rather than
/// auto-derived from OpVisit.ClinicalOrder.Description — the two modules stay loosely
/// coupled, same deliberate choice as Billing's OP-visit link (docs/modules/lab.md).
/// EmergencyVisitId (Phase 16) is the same kind of loose, optional link — a STAT order from
/// ER is just a LabOrder with PatientId set and Priority=Stat; this FK is traceability only.
/// </summary>
public class LabOrder : BranchEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long? OpVisitId { get; set; }
    public OpVisit? OpVisit { get; set; }
    public long? EmergencyVisitId { get; set; }
    public EmergencyVisit? EmergencyVisit { get; set; }
    /// <summary>Set for inpatient investigations so the test is charged to the admission's IP bill.</summary>
    public long? AdmissionId { get; set; }
    public Prixo.Domain.Entities.Ipd.Admission? Admission { get; set; }
    /// <summary>Traceability only — completing/cancelling this order does not require a matching ClinicalOrder to exist.</summary>
    public long? ClinicalOrderId { get; set; }
    public ClinicalOrder? ClinicalOrder { get; set; }
    public long? OrderedByDoctorId { get; set; }
    public Doctor? OrderedByDoctor { get; set; }

    public required string OrderNumber { get; set; }
    public DateOnly OrderDate { get; set; }
    public ClinicalOrderPriority Priority { get; set; } = ClinicalOrderPriority.Routine;
    public LabOrderStatus Status { get; set; } = LabOrderStatus.Ordered;
    public string? ClinicalNotes { get; set; }
    public string? CancelReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public ICollection<LabOrderItem> Items { get; set; } = [];
}

public class LabOrderItem : BaseEntity
{
    public long LabOrderId { get; set; }
    public LabOrder? LabOrder { get; set; }
    public long LabTestId { get; set; }
    public LabTest? LabTest { get; set; }
    /// <summary>Set when this item was expanded from a TestProfile, purely so the UI can group them back together.</summary>
    public long? TestProfileId { get; set; }
    public TestProfile? TestProfile { get; set; }
    public long? SampleId { get; set; }
    public Sample? Sample { get; set; }
    /// <summary>Soft link to billing.bill_items — set once billed; not an enforced gate this phase.</summary>
    public long? BillItemId { get; set; }
    public LabOrderItemStatus Status { get; set; } = LabOrderItemStatus.Ordered;

    public LabResult? Result { get; set; }
}
