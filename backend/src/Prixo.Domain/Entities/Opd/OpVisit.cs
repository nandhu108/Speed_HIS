using Prixo.Domain.Common;
using Prixo.Domain.Entities.Appointments;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Opd;

/// <summary>
/// OPD visit header — the front-desk registration record and the anchor for the whole
/// consultation workspace. ConsultationFee is the "draft charge" the plan calls for;
/// Phase 6 Billing turns it into a real bill line, not built here.
/// </summary>
public class OpVisit : BranchEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    /// <summary>Set for Category == Opd visits; null for Category == Mhc (see MhcDoctorId).</summary>
    public long? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    /// <summary>Clinical department the patient is being seen under (e.g. General Medicine).
    /// Mandatory when registering an OPD visit; nullable only for MHC rows and visits that
    /// pre-date the field.</summary>
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    /// <summary>Set when the visit originated from a checked-in appointment.</summary>
    public long? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    /// <summary>Discriminates the OPD consultation flow from the MHC checkup-registration flow
    /// sharing this table (front-office extension, no separate MhcVisit table/engine).</summary>
    public VisitCategory Category { get; set; } = VisitCategory.Opd;
    /// <summary>Set for Category == Mhc instead of DoctorId — MHC panel doctors are a separate
    /// tagging list (MhcDoctor), not the regular Doctor roster.</summary>
    public long? MhcDoctorId { get; set; }
    public MhcDoctor? MhcDoctor { get; set; }
    /// <summary>Set for Category == Mhc — the checkup package priced at registration (snapshot
    /// into ConsultationFee, same as OPD's fee snapshot).</summary>
    public long? MhcPackageId { get; set; }
    public MhcPackage? MhcPackage { get; set; }
    /// <summary>Optional outreach-event tag, independent of Category — a health camp visit is
    /// still an ordinary Opd (or Mhc) visit with the same doctor/fee/New-Review logic, just
    /// tagged to the camp it was registered under.</summary>
    public long? CampId { get; set; }
    public Camp? Camp { get; set; }

    public required string VisitNumber { get; set; }
    public DateOnly VisitDate { get; set; }
    public int TokenNumber { get; set; }
    /// <summary>Auto-detected from Doctor.FollowUpWindowDays (Opd) or MhcDoctor.FollowUpWindowDays
    /// (Mhc) against the patient's visit history.</summary>
    public VisitType VisitType { get; set; }
    public OpVisitStatus Status { get; set; } = OpVisitStatus.Waiting;
    public decimal ConsultationFee { get; set; }

    /// <summary>Explicit registration-time field alongside the existing AppointmentId-presence
    /// inference — additive, not consumed by the slot/queue engine.</summary>
    public AppointmentBookingType BookingType { get; set; } = AppointmentBookingType.WalkIn;
    /// <summary>Purely descriptive OP registration tag, same unwired posture as AppointmentType.</summary>
    public long? VisitClassId { get; set; }
    public VisitClass? VisitClass { get; set; }
    /// <summary>Purely descriptive OP registration tag (Morning/Afternoon/Evening).</summary>
    public long? SessionId { get; set; }
    public VisitSession? Session { get; set; }
    /// <summary>Plain tag marking this OP episode as medico-legal — no statutory register
    /// wiring here; MLC's real workflow stays in Er.MlcRecord (Emergency module).</summary>
    public bool IsMlcCase { get; set; }

    public DateTimeOffset? InConsultAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CancelReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}
