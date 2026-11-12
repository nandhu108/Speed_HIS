namespace Prixo.Application.Features.Portal;

public record SubmitFeedbackRequest(long? OpVisitId, long? AdmissionId, int Rating, string? Comments);
public record FeedbackDto(long Id, long? OpVisitId, long? AdmissionId, int Rating, string? Comments, DateTimeOffset SubmittedAt);
