using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Ot;

// ── Surgical safety checklist ────────────────────────────────────────────
public record UpdateSignInRequest(
    bool PatientIdentityConfirmed, bool SiteMarked, bool AnaesthesiaSafetyCheckComplete, bool PulseOximeterFunctioning,
    bool KnownAllergy, bool DifficultAirwayRisk, bool RiskOfBloodLoss, bool Complete);

public record UpdateTimeOutRequest(
    bool TeamIntroduced, bool PatientSiteProcedureConfirmed, bool CriticalEventsReviewed,
    bool AntibioticProphylaxisGiven, bool ImagingDisplayed, bool Complete);

public record UpdateSignOutRequest(
    bool ProcedureNameRecorded, bool InstrumentSpongeNeedleCountCorrect, bool SpecimenLabeled,
    bool EquipmentProblemsAddressed, bool RecoveryConcernsReviewed, bool Complete);

public record ChecklistDto(
    long BookingId,
    bool PatientIdentityConfirmed, bool SiteMarked, bool AnaesthesiaSafetyCheckComplete, bool PulseOximeterFunctioning,
    bool KnownAllergy, bool DifficultAirwayRisk, bool RiskOfBloodLoss, DateTimeOffset? SignInCompletedAt,
    bool TeamIntroduced, bool PatientSiteProcedureConfirmed, bool CriticalEventsReviewed,
    bool AntibioticProphylaxisGiven, bool ImagingDisplayed, DateTimeOffset? TimeOutCompletedAt,
    bool ProcedureNameRecorded, bool InstrumentSpongeNeedleCountCorrect, bool SpecimenLabeled,
    bool EquipmentProblemsAddressed, bool RecoveryConcernsReviewed, DateTimeOffset? SignOutCompletedAt);

// ── Pre-anaesthesia / intra-op ───────────────────────────────────────────
public record SavePreAnaesthesiaRequest(
    long AssessedByDoctorId, string AsaGrade, string? AirwayAssessment, string? Comorbidities,
    string? Investigations, string? AnaesthesiaPlan, string? Notes);

public record PreAnaesthesiaDto(
    long Id, long BookingId, long AssessedByDoctorId, string AssessedByDoctorName, DateTimeOffset AssessedAt,
    string AsaGrade, string? AirwayAssessment, string? Comorbidities, string? Investigations, string? AnaesthesiaPlan, string? Notes);

public record SaveIntraOpRequest(
    AnaesthesiaType AnaesthesiaType, long AdministeredByDoctorId, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, string? Notes);

public record IntraOpDto(
    long Id, long BookingId, AnaesthesiaType AnaesthesiaType, long AdministeredByDoctorId, string AdministeredByDoctorName,
    DateTimeOffset StartedAt, DateTimeOffset? EndedAt, string? Notes);

// ── Operative / post-op notes ────────────────────────────────────────────
public record SaveOperativeNoteRequest(
    long SurgeonId, string ProcedurePerformed, string? Findings, string? Complications, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, string? Notes);

public record OperativeNoteDto(
    long Id, long BookingId, long SurgeonId, string SurgeonName, string ProcedurePerformed,
    string? Findings, string? Complications, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, string? Notes);

public record SavePostOpNoteRequest(string? RecoveryNotes, string? VitalsSummary, string? Complications, DateTimeOffset? ShiftedAt, string? Notes);

public record PostOpNoteDto(
    long Id, long BookingId, string? RecoveryNotes, string? VitalsSummary, string? Complications, DateTimeOffset? ShiftedAt, string? Notes);
