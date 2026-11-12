using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Ot;

/// <summary>
/// A scheduled surgery against an admission (plan: OT "needs IPD + stock + billing" — every
/// booking is IPD-anchored, no OP/day-care path). SurgeonId/AnaesthetistId are direct FKs
/// (not SurgeryTeamMember rows) because they're exactly what BookingService's conflict
/// check needs to query against; additional team members (assistant surgeon, scrub nurse…)
/// go in the Items collection instead. PatientConsentId mirrors Admission's own
/// "set once the shared PatientConsent flow is signed" pattern — BookingService gates the
/// Scheduled→InProgress transition on it being set, same posture as Radiology's contrast
/// consent gate.
/// </summary>
public class SurgeryBooking : BranchEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    public long TheatreId { get; set; }
    public Theatre? Theatre { get; set; }

    public required string BookingNumber { get; set; }
    public required string ProcedureName { get; set; }

    public long SurgeonId { get; set; }
    public Doctor? Surgeon { get; set; }
    public long? AnaesthetistId { get; set; }
    public Doctor? Anaesthetist { get; set; }

    public DateTimeOffset ScheduledStart { get; set; }
    public DateTimeOffset ScheduledEnd { get; set; }
    public DateTimeOffset? ActualStart { get; set; }
    public DateTimeOffset? ActualEnd { get; set; }

    public SurgeryStatus Status { get; set; } = SurgeryStatus.Scheduled;
    /// <summary>Reason text for Cancelled/Postponed — required by the service on those transitions only.</summary>
    public string? StatusReason { get; set; }

    public long? PatientConsentId { get; set; }
    public PatientConsent? PatientConsent { get; set; }
    public DateTimeOffset? ConsentVerifiedAt { get; set; }
    public long? ConsentVerifiedByUserId { get; set; }
    public User? ConsentVerifiedByUser { get; set; }

    public string? Notes { get; set; }

    public ICollection<SurgeryTeamMember> TeamMembers { get; set; } = [];
}

/// <summary>Team roles beyond the booking's own Surgeon/Anaesthetist fields. DoctorId is set for a doctor team member; StaffName covers non-doctor staff (scrub/circulating nurses) since they aren't in the Doctor master.</summary>
public class SurgeryTeamMember : BaseEntity
{
    public long BookingId { get; set; }
    public SurgeryBooking? Booking { get; set; }
    public SurgeryTeamRole Role { get; set; }
    public long? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public string? StaffName { get; set; }
}
