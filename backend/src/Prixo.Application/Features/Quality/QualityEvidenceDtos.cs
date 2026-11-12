namespace Prixo.Application.Features.Quality;

public record UploadEvidenceRequest(long StandardId, string Title, string? Description);
public record EvidenceDto(long Id, long StandardId, string StandardCode, string Title, string? Description, string FileName, string UploadedByName, DateTimeOffset UploadedAt);
public record EvidenceQuery(long? StandardId);
