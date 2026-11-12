namespace Prixo.Application.Features.Emr;

/// <summary>Discriminator for the unified patient timeline — a projection-only concept, not a persisted domain enum.</summary>
public enum TimelineEventType
{
    Visit = 1,
    Admission = 2,
    LabResult = 3,
    Prescription = 4,
    Procedure = 5,
    DischargeSummary = 6,
    ClinicalNote = 7,
    Attachment = 8,
}

public record TimelineEventDto(DateTimeOffset At, TimelineEventType Type, string Title, string Summary, long LinkId);

public record PrescriptionHistoryItemDto(long DrugId, string DrugName, string Dose, string Frequency, string Duration);
public record PrescriptionHistoryDto(long VisitId, DateOnly VisitDate, string DoctorName, IReadOnlyList<PrescriptionHistoryItemDto> Items);

public record InvestigationHistoryDto(long LabOrderItemId, long LabOrderId, DateOnly OrderDate, string TestName, string Status, string? ResultSummary);

public record ProcedureHistoryDto(long AdmissionId, string AdmissionNumber, string ProcedureName, DateTimeOffset PerformedAt, string PerformedByDoctorName, string? Findings);

public record DischargeSummaryArchiveDto(long AdmissionId, string AdmissionNumber, DateTimeOffset? DischargedAt, string FinalDiagnosis, bool IsApproved);

public record ResultsTrendPointDto(DateOnly Date, decimal Value, string Flag);
public record ResultsTrendSeriesDto(long ParameterId, string ParameterName, string? Unit, IReadOnlyList<ResultsTrendPointDto> Points);
