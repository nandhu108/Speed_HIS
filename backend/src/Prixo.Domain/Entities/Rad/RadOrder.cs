using Prixo.Domain.Common;
using Prixo.Domain.Entities.Er;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Rad;

/// <summary>
/// Radiology order header — created from an OPD visit, an IPD admission, an ER visit
/// (Phase 16), or directly (plan §12 "from OPD/IPD/ER/direct"), mirroring Lab's LabOrder
/// shape exactly. Every link is optional/loose — only PatientId is required.
/// </summary>
public class RadOrder : BranchEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long? OpVisitId { get; set; }
    public OpVisit? OpVisit { get; set; }
    public long? AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    public long? EmergencyVisitId { get; set; }
    public EmergencyVisit? EmergencyVisit { get; set; }
    /// <summary>Traceability only — completing/cancelling this order does not require a matching ClinicalOrder to exist.</summary>
    public long? ClinicalOrderId { get; set; }
    public ClinicalOrder? ClinicalOrder { get; set; }
    public long? OrderedByDoctorId { get; set; }
    public Doctor? OrderedByDoctor { get; set; }

    public required string OrderNumber { get; set; }
    public DateOnly OrderDate { get; set; }
    public ClinicalOrderPriority Priority { get; set; } = ClinicalOrderPriority.Routine;
    public RadOrderStatus Status { get; set; } = RadOrderStatus.Ordered;
    public string? ClinicalNotes { get; set; }
    public string? CancelReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public ICollection<RadOrderItem> Items { get; set; } = [];
}

/// <summary>One per ordered test. AccessionNumber/PacsStudyUid are the PACS/DICOM placeholder columns per plan §12 — only AccessionNumber is ever actually generated.</summary>
public class RadOrderItem : BaseEntity
{
    public long RadOrderId { get; set; }
    public RadOrder? RadOrder { get; set; }
    public long RadiologyTestId { get; set; }
    public RadiologyTest? RadiologyTest { get; set; }
    public RadOrderItemStatus Status { get; set; } = RadOrderItemStatus.Ordered;

    public required string AccessionNumber { get; set; }
    /// <summary>PACS/DICOM placeholder — never set by application code (plan §12: "placeholder only").</summary>
    public string? PacsStudyUid { get; set; }

    public DateTimeOffset? ImageCapturedAt { get; set; }
    public long? ImageCapturedByUserId { get; set; }

    /// <summary>
    /// Soft, unenforced link to billing.bill_items — same posture as Lab's LabOrderItem.BillItemId
    /// (added 2026-09-16, alongside Lab's, closing docs/PATIENT_JOURNEY_GAP_ANALYSIS.md §4.3).
    /// Nothing in RadOrderService blocks scheduling/capture/report entry/approval on it being null.
    /// </summary>
    public long? BillItemId { get; set; }

    public RadSchedule? Schedule { get; set; }
    public ContrastConsent? ContrastConsent { get; set; }
    public RadReport? Report { get; set; }
}
