namespace Prixo.Application.Features.Mrd;

public record SaveFileAuditRequest(
    bool DischargeSummarySigned, bool ConsentsPresent, bool ProgressNotesComplete,
    bool InvestigationReportsPresent, bool NursingRecordsComplete, string? MissingDocumentsNotes);

public record FileAuditDto(
    long Id, long AdmissionId, bool DischargeSummarySigned, bool ConsentsPresent, bool ProgressNotesComplete,
    bool InvestigationReportsPresent, bool NursingRecordsComplete, bool IsComplete, string? MissingDocumentsNotes,
    string AuditedByName, DateTimeOffset AuditedAt);
