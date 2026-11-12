using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Er;

public record SaveAssessmentRequest(
    long AssessedByDoctorId, string? History, string? ExaminationFindings, string? ProvisionalDiagnosis, string? TreatmentGiven,
    DispositionType? Disposition, string? DispositionNotes, string? TransferredToFacility, string? TransferSummary);

public record AssessmentDto(
    long Id, long EmergencyVisitId, long AssessedByDoctorId, string AssessedByDoctorName, DateTimeOffset AssessedAt,
    string? History, string? ExaminationFindings, string? ProvisionalDiagnosis, string? TreatmentGiven,
    DispositionType? Disposition, string? DispositionNotes, string? TransferredToFacility, string? TransferSummary);

public record AllocateObservationRequest(long BedId);

public record ObservationAllocationDto(
    long Id, long EmergencyVisitId, long BedId, string WardName, string RoomName, string BedCode,
    DateTimeOffset AllocatedAt, DateTimeOffset? ReleasedAt);
