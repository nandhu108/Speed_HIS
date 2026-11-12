namespace Prixo.Application.Features.Quality;

public record SaveCommitteeActionItemRequest(string Description, long OwnerUserId, DateOnly DueDate);
public record SaveCommitteeMeetingRequest(
    long CommitteeId, DateOnly MeetingDate, string Minutes, string? AttendeesText, IReadOnlyList<SaveCommitteeActionItemRequest> ActionItems);

public record CommitteeActionItemDto(long Id, string Description, string OwnerName, DateOnly DueDate, bool IsCompleted, DateTimeOffset? CompletedAt);
public record CommitteeMeetingDto(
    long Id, long CommitteeId, string CommitteeName, DateOnly MeetingDate, string Minutes, string? AttendeesText,
    IReadOnlyList<CommitteeActionItemDto> ActionItems);
