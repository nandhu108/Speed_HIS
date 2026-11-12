using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;

namespace Prixo.Domain.Entities.Ot;

/// <summary>
/// The WHO Surgical Safety Checklist's three fixed phases (Sign In/Time Out/Sign Out) —
/// one row per booking, created empty alongside it. Each phase is a discrete set of
/// standard checkboxes plus a completion stamp; BookingService gates the booking's status
/// transitions on the matching phase being completed (Sign In before InProgress, Sign Out
/// before Completed) — same "checklist gates workflow" posture as IPD's DischargeClearance.
/// </summary>
public class SurgicalSafetyChecklist : BaseEntity
{
    public long BookingId { get; set; }
    public SurgeryBooking? Booking { get; set; }

    // Sign In — before induction of anaesthesia
    public bool PatientIdentityConfirmed { get; set; }
    public bool SiteMarked { get; set; }
    public bool AnaesthesiaSafetyCheckComplete { get; set; }
    public bool PulseOximeterFunctioning { get; set; }
    public bool KnownAllergy { get; set; }
    public bool DifficultAirwayRisk { get; set; }
    public bool RiskOfBloodLoss { get; set; }
    public DateTimeOffset? SignInCompletedAt { get; set; }
    public long? SignInCompletedByUserId { get; set; }
    public User? SignInCompletedByUser { get; set; }

    // Time Out — before skin incision
    public bool TeamIntroduced { get; set; }
    public bool PatientSiteProcedureConfirmed { get; set; }
    public bool CriticalEventsReviewed { get; set; }
    public bool AntibioticProphylaxisGiven { get; set; }
    public bool ImagingDisplayed { get; set; }
    public DateTimeOffset? TimeOutCompletedAt { get; set; }
    public long? TimeOutCompletedByUserId { get; set; }
    public User? TimeOutCompletedByUser { get; set; }

    // Sign Out — before patient leaves the operating room
    public bool ProcedureNameRecorded { get; set; }
    public bool InstrumentSpongeNeedleCountCorrect { get; set; }
    public bool SpecimenLabeled { get; set; }
    public bool EquipmentProblemsAddressed { get; set; }
    public bool RecoveryConcernsReviewed { get; set; }
    public DateTimeOffset? SignOutCompletedAt { get; set; }
    public long? SignOutCompletedByUserId { get; set; }
    public User? SignOutCompletedByUser { get; set; }

    public string? Notes { get; set; }
}
