using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Nursing;

public record PatientHandoverNoteRequest(long AdmissionId, string Situation, string Background, string Assessment, string Recommendation);
public record CreateShiftHandoverRequest(
    long WardId, DateOnly ShiftDate, NursingShift Shift, long? HandoverToUserId, string? GeneralNotes,
    List<PatientHandoverNoteRequest> PatientNotes);

public record PatientHandoverNoteDto(
    long Id, long AdmissionId, string AdmissionNumber, string PatientName, string Situation, string Background, string Assessment, string Recommendation);

public record ShiftHandoverListItemDto(
    long Id, long WardId, string WardName, DateOnly ShiftDate, NursingShift Shift, string HandoverFromName,
    string? HandoverToName, DateTimeOffset HandedOverAt, int PatientCount);

public record ShiftHandoverDetailDto(
    long Id, long WardId, string WardName, DateOnly ShiftDate, NursingShift Shift, string HandoverFromName,
    string? HandoverToName, string? GeneralNotes, DateTimeOffset HandedOverAt, IReadOnlyList<PatientHandoverNoteDto> PatientNotes);

/// <summary>Prefill helper — the ward's currently-admitted patients, so the handover form doesn't need a separate bed-board lookup.</summary>
public record WardPatientDto(long AdmissionId, string AdmissionNumber, string PatientName, string BedCode);
