using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Quality;

public record RaiseCapaRequest(CapaSourceType SourceType, long SourceId, string ActionPlan, long OwnerUserId, DateOnly DueDate);
public record VerifyCapaRequest(string? VerificationNotes);

public record CapaDto(
    long Id, CapaSourceType SourceType, long SourceId, string ActionPlan, string OwnerName, DateOnly DueDate,
    CapaStatus Status, DateTimeOffset? CompletedAt, string? VerifiedByName, DateTimeOffset? VerifiedAt, string? VerificationNotes);

public record CapaQuery(CapaStatus? Status, CapaSourceType? SourceType);
