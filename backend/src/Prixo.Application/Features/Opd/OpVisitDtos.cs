using Prixo.Application.Common.Models;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Opd;

public record RegisterOpVisitRequest(
    long PatientId, long DoctorId, long? AppointmentId, long? CampId = null,
    AppointmentBookingType BookingType = AppointmentBookingType.WalkIn,
    long? VisitClassId = null, long? SessionId = null, bool IsMlcCase = false,
    decimal? AdvanceAmount = null, long? PaymentModeId = null, string? AdvanceReferenceNumber = null,
    long? DepartmentId = null);

public record RegisterMhcVisitRequest(long PatientId, long MhcDoctorId, long MhcPackageId);

public record UpdateVisitCampRequest(long? CampId);

/// <summary>Result of a CSV bulk import (Camp or MHC registrations) — one row per UHID, all sharing the same doctor/camp/package for the batch.</summary>
public record BulkImportResultDto(int Inserted, int Skipped, IReadOnlyList<string> Errors);

public class OpVisitQuery : PagedQuery
{
    public VisitCategory Category { get; set; } = VisitCategory.Opd;
    public long? DoctorId { get; set; }
    public long? PatientId { get; set; }
    public long? CampId { get; set; }
    public DateOnly? Date { get; set; }
    public OpVisitStatus? Status { get; set; }
}

public record OpVisitListItemDto(
    long Id, string VisitNumber, int TokenNumber, long PatientId, string PatientUhid, string PatientName,
    long DoctorId, string DoctorName, DateOnly VisitDate, VisitType VisitType, OpVisitStatus Status, bool AlreadyAdmitted,
    long? CampId, string? CampName);

public record OpVisitDetailDto(
    long Id, string VisitNumber, int TokenNumber, long PatientId, string PatientUhid, string PatientName,
    string PatientMobile, string PatientGender, string PatientAgeDisplay, long DoctorId, string DoctorName,
    long? AppointmentId, DateOnly VisitDate, VisitType VisitType, OpVisitStatus Status, decimal ConsultationFee,
    DateTimeOffset? InConsultAt, DateTimeOffset? CompletedAt, string? CancelReason, DateTimeOffset? CancelledAt,
    long? CampId, string? CampName,
    AppointmentBookingType BookingType, long? VisitClassId, string? VisitClassName,
    long? SessionId, string? SessionName, bool IsMlcCase,
    long? DepartmentId = null, string? DepartmentName = null);

public record CancelVisitRequest(string Reason);

public record QueueEntryDto(
    long VisitId, int TokenNumber, long PatientId, string PatientName, string PatientUhid,
    long DoctorId, string DoctorName, VisitType VisitType, OpVisitStatus Status);

// ── Clinical summary (read-only context for the workspace) ─────────────────
public record RecentVisitSummaryDto(long Id, DateOnly VisitDate, string DoctorName, IReadOnlyList<string> Diagnoses);
public record RecentMedicationDto(string DrugName, string Dose, string Frequency, DateOnly PrescribedOn);

// ── Vitals ───────────────────────────────────────────────────────────────
public record VisitVitalsDto(
    long? Id, decimal? HeightCm, decimal? WeightKg, decimal? Bmi, decimal? TemperatureF,
    int? PulseRate, int? RespiratoryRate, int? BloodPressureSystolic, int? BloodPressureDiastolic,
    int? SpO2, decimal? RandomBloodSugar);
public record SaveVitalsRequest(
    decimal? HeightCm, decimal? WeightKg, decimal? TemperatureF,
    int? PulseRate, int? RespiratoryRate, int? BloodPressureSystolic, int? BloodPressureDiastolic,
    int? SpO2, decimal? RandomBloodSugar);

// ── Complaints ───────────────────────────────────────────────────────────
public record VisitComplaintDto(long Id, string Complaint, string? Duration);
public record SaveComplaintRequest(string Complaint, string? Duration);
public record SaveComplaintsRequest(IReadOnlyList<SaveComplaintRequest> Complaints);

// ── Examination ──────────────────────────────────────────────────────────
public record VisitExaminationDto(string? HistoryNotes, string? ExaminationNotes);
public record SaveExaminationRequest(string? HistoryNotes, string? ExaminationNotes);

// ── Diagnoses ────────────────────────────────────────────────────────────
public record VisitDiagnosisDto(long Id, long IcdCodeId, string IcdCode, string IcdDescription, DiagnosisType Type, string? Notes);
public record SaveDiagnosisRequest(long IcdCodeId, DiagnosisType Type, string? Notes);
public record SaveDiagnosesRequest(IReadOnlyList<SaveDiagnosisRequest> Diagnoses);

// ── Prescription ─────────────────────────────────────────────────────────
public record PrescriptionItemDto(
    long Id, long DrugId, string DrugName, string Dose, string Frequency, string Duration, string? Route, string? Instructions);
public record SavePrescriptionItemRequest(
    long DrugId, string Dose, string Frequency, string Duration, string? Route, string? Instructions);
public record PrescriptionDto(long? Id, string? Notes, IReadOnlyList<PrescriptionItemDto> Items);
public record SavePrescriptionRequest(string? Notes, IReadOnlyList<SavePrescriptionItemRequest> Items);

// ── Orders ───────────────────────────────────────────────────────────────
public record ClinicalOrderDto(
    long Id, ClinicalOrderType OrderType, string Description, ClinicalOrderPriority Priority,
    ClinicalOrderStatus Status, string? Notes);
public record AddClinicalOrderRequest(
    ClinicalOrderType OrderType, string Description, ClinicalOrderPriority Priority, string? Notes);

// ── Referrals ────────────────────────────────────────────────────────────
public record VisitReferralDto(
    long Id, ReferralDirection Direction, long? ReferredToDoctorId, string? ReferredToDoctorName,
    string? ExternalDetails, string Reason, string? OpinionNotes, DateTimeOffset? OpinionEnteredAt);
public record AddVisitReferralRequest(
    ReferralDirection Direction, long? ReferredToDoctorId, string? ExternalDetails, string Reason);

/// <summary>"Doctor Opinion Entry" — the referred-to doctor's cross-consult response to an
/// Internal referral.</summary>
public record SaveReferralOpinionRequest(string OpinionNotes);

/// <summary>One row in a doctor's cross-consult inbox — surfaced via the Doctor Portal, but the
/// data lives entirely in `opd.referrals`.</summary>
public record OpinionRequestDto(
    long ReferralId, long VisitId, string VisitNumber, DateOnly VisitDate,
    long PatientId, string PatientUhid, string PatientName,
    string ReferringDoctorName, string Reason, string? OpinionNotes, DateTimeOffset? OpinionEnteredAt);

// ── Follow-up ────────────────────────────────────────────────────────────
public record FollowUpAdviceDto(long? Id, DateOnly? FollowUpDate, string? Notes);
public record SaveFollowUpRequest(DateOnly FollowUpDate, string? Notes);

// ── Workspace aggregation ────────────────────────────────────────────────
public record ConsultationWorkspaceDto(
    OpVisitDetailDto Visit,
    IReadOnlyList<RecentVisitSummaryDto> RecentVisits,
    IReadOnlyList<RecentMedicationDto> RecentMedications,
    VisitVitalsDto? Vitals,
    IReadOnlyList<VisitComplaintDto> Complaints,
    VisitExaminationDto? Examination,
    IReadOnlyList<VisitDiagnosisDto> Diagnoses,
    PrescriptionDto? Prescription,
    IReadOnlyList<ClinicalOrderDto> Orders,
    IReadOnlyList<VisitReferralDto> Referrals,
    FollowUpAdviceDto? FollowUp);

// ── Prescription templates ───────────────────────────────────────────────
public record PrescriptionTemplateItemDto(
    long Id, long DrugId, string DrugName, string Dose, string Frequency, string Duration, string? Route, string? Instructions);
public record SaveTemplateItemRequest(
    long DrugId, string Dose, string Frequency, string Duration, string? Route, string? Instructions);
public record PrescriptionTemplateDto(long Id, long DoctorId, string Name, IReadOnlyList<PrescriptionTemplateItemDto> Items);
public record SavePrescriptionTemplateRequest(long DoctorId, string Name, IReadOnlyList<SaveTemplateItemRequest> Items);
