using Prixo.Domain.Common;
using Prixo.Domain.Entities.Er;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Appointments;

/// <summary>
/// A booked or walk-in slot. Token number is generated atomically per doctor+date via
/// ISequenceService and denormalized here rather than in a separate table — this phase
/// has no other consumer of a standalone token record.
/// </summary>
public class Appointment : BranchEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int TokenNumber { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public AppointmentBookingType BookingType { get; set; } = AppointmentBookingType.Booked;
    /// <summary>Optional hospital-defined categorization tag ("Appointment Masters") — unrelated to
    /// BookingType and not consumed by slot resolution.</summary>
    public long? AppointmentTypeId { get; set; }
    public AppointmentType? AppointmentType { get; set; }

    public string? Notes { get; set; }
    public string? CancelReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? CheckedInAt { get; set; }
    public DateTimeOffset? NoShowMarkedAt { get; set; }

    /// <summary>Set on the new appointment created by a reschedule; the old one is cancelled.</summary>
    public long? RescheduledFromId { get; set; }
    public Appointment? RescheduledFrom { get; set; }

    /// <summary>"Skip Token" — patient not present when called. Status stays CheckedIn; the queue
    /// sorts a skipped token to the back instead of removing it. SkipCount is a running counter
    /// across repeated skip/recall cycles; SkippedAt null means the token is in normal rotation.</summary>
    public DateTimeOffset? SkippedAt { get; set; }
    public int SkipCount { get; set; }

    /// <summary>"Token Transfer" — single-hop lineage recorded when the token is reassigned to a
    /// different doctor mid-day, same posture as RescheduledFromId (last hop only).</summary>
    public long? TransferredFromDoctorId { get; set; }
    public Doctor? TransferredFromDoctor { get; set; }
    public DateTimeOffset? TransferredAt { get; set; }

    /// <summary>"ER Token Mapping" — loose link when this token was minted for an ER patient once
    /// assigned a doctor, same posture as Lab/Rad/Billing's EmergencyVisitId columns.</summary>
    public long? EmergencyVisitId { get; set; }
    public EmergencyVisit? EmergencyVisit { get; set; }

    /// <summary>"Tele-consulting" — an additive, unwired categorization tag exactly like
    /// AppointmentTypeId; plays no role in slot resolution. TeleconsultLink is a plain external
    /// meeting-URL string pasted in by staff (no video-provider integration exists — Communication
    /// is a held module per plan §21), settable at booking or later via a dedicated action.</summary>
    public ConsultationMode ConsultationMode { get; set; } = ConsultationMode.InPerson;
    public string? TeleconsultLink { get; set; }
}
