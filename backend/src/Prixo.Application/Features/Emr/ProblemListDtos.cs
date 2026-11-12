using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Emr;

public record AddProblemRequest(long? IcdCodeId, string DiagnosisText, DateOnly IdentifiedDate, string? Notes);
public record ProblemListEntryDto(
    long Id, long? IcdCodeId, string? IcdCode, string DiagnosisText, ProblemStatus Status,
    DateOnly IdentifiedDate, DateOnly? ResolvedDate, string? Notes, string RecordedByName);
