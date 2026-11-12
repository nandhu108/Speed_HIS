namespace Prixo.Application.Features.Emr;

public record AddClinicalNoteRequest(string NoteType, string? Title, string Content, long? OpVisitId, long? AdmissionId);
public record ClinicalNoteDto(
    long Id, string NoteType, string? Title, string Content, DateTimeOffset NoteAt, string RecordedByName,
    long? OpVisitId, long? AdmissionId);
