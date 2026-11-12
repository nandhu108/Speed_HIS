using Prixo.Domain.Common;
using Prixo.Domain.Entities.Er;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Ipd;

/// <summary>
/// Admission header — the IPD anchor, mirroring OpVisit's role for OPD. CurrentBedId is a
/// denormalized pointer to the active BedAllocation's bed for cheap reads; BedAllocations
/// holds the full transfer history. ReferringOpVisitId/ReferringEmergencyVisitId are loose,
/// optional links (same traceability-only posture as Lab's ClinicalOrderId/Pharmacy's
/// PrescriptionId) — nothing auto-creates an admission from an OPD visit or an ER visit
/// (plan §16 "Admission Conversion (ER → IPD carrying clinical data)" is a front-desk using
/// this existing admission flow and setting this link, not a new conversion endpoint).
/// </summary>
public class Admission : BranchEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public required string AdmissionNumber { get; set; }
    public DateTimeOffset AdmissionDate { get; set; }
    public AdmissionType AdmissionType { get; set; } = AdmissionType.Planned;

    public long PrimaryConsultantId { get; set; }
    public Doctor? PrimaryConsultant { get; set; }

    /// <summary>Clinical department the patient is admitted under. Mandatory at admission;
    /// nullable only for admissions that pre-date the field.</summary>
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public long? ReferringOpVisitId { get; set; }
    public OpVisit? ReferringOpVisit { get; set; }
    public long? ReferringEmergencyVisitId { get; set; }
    public EmergencyVisit? ReferringEmergencyVisit { get; set; }

    public long CurrentBedId { get; set; }
    public Bed? CurrentBed { get; set; }

    /// <summary>
    /// Optional exposure ceiling set at admission (front desk's "IP Credit Limit") — a warning
    /// threshold on (running IP bill balance − available advances), never a hard block, matching
    /// the codebase's post-facto-friendly posture elsewhere (e.g. Emergency's empty bill).
    /// </summary>
    public decimal? CreditLimit { get; set; }

    public string? ProvisionalDiagnosis { get; set; }

    /// <summary>Set once the admission consent (captured via the shared PatientConsent flow) is signed.</summary>
    public long? PatientConsentId { get; set; }
    public PatientConsent? PatientConsent { get; set; }

    public AdmissionStatus Status { get; set; } = AdmissionStatus.Admitted;

    public DateTimeOffset? DischargeRequestedAt { get; set; }
    public long? DischargeRequestedByDoctorId { get; set; }
    public Doctor? DischargeRequestedByDoctor { get; set; }
    public DischargeType? DischargeType { get; set; }
    /// <summary>Free text for the discharge type's context — referral destination, cause of death, LAMA reason…</summary>
    public string? DischargeTypeNotes { get; set; }
    public DateTimeOffset? DischargedAt { get; set; }
    public string? CancelReason { get; set; }

    public ICollection<AdmissionConsultant> Consultants { get; set; } = [];
    public ICollection<BedAllocation> BedAllocations { get; set; } = [];
    public ICollection<DischargeClearance> Clearances { get; set; } = [];
}
