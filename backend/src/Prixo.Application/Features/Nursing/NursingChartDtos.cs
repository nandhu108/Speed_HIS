using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Nursing;

public record AddNursingAssessmentRequest(string TemplateCode, string DataJson);
public record NursingAssessmentDto(long Id, string TemplateCode, DateTimeOffset AssessedAt, string AssessedByName, string DataJson);

public record AddNursingNoteRequest(NursingShift Shift, string Notes);
public record NursingNoteDto(long Id, NursingShift Shift, DateTimeOffset NoteAt, string RecordedByName, string Notes);

public record AddRiskAssessmentRequest(RiskAssessmentType AssessmentType, Dictionary<string, int> ItemScores);
public record RiskAssessmentDto(
    long Id, RiskAssessmentType AssessmentType, DateTimeOffset AssessedAt, string AssessedByName,
    Dictionary<string, int> ItemScores, int TotalScore, string RiskLevel);

public record AddCarePlanRequest(string ProblemStatement, string Goal, string Interventions);
public record ResolveCarePlanRequest(string? EvaluationNotes);
public record CarePlanDto(
    long Id, string ProblemStatement, string Goal, string Interventions, CarePlanStatus Status,
    string CreatedByName, DateTimeOffset CreatedAt, string? EvaluationNotes, DateTimeOffset? ResolvedAt);

public record AddTransferNoteRequest(long? BedAllocationId, string HandoverNotes, long? ReceivedByUserId);
public record TransferNoteDto(
    long Id, DateTimeOffset TransferredAt, string HandoverNotes, string TransferredByName, string? ReceivedByName);
