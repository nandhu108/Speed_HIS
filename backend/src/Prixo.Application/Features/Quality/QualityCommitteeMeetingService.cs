using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Quality;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Quality;

/// <summary>Committee Meeting Records (plan §20: "minutes, action items"). Action items are a shallow child list, deliberately not routed through the CAPA tracker.</summary>
public class QualityCommitteeMeetingService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> CreateAsync(SaveCommitteeMeetingRequest request, CancellationToken ct = default)
    {
        if (!await db.QualityCommittees.AnyAsync(c => c.Id == request.CommitteeId, ct))
        {
            throw new NotFoundException(nameof(QualityCommittee), request.CommitteeId);
        }
        var meeting = new QualityCommitteeMeeting
        {
            BranchId = RequireBranch(), CommitteeId = request.CommitteeId, MeetingDate = request.MeetingDate,
            Minutes = request.Minutes, AttendeesText = request.AttendeesText,
            ActionItems = request.ActionItems.Select(a => new QualityCommitteeActionItem
            {
                Description = a.Description, OwnerUserId = a.OwnerUserId, DueDate = a.DueDate,
            }).ToList(),
        };
        db.QualityCommitteeMeetings.Add(meeting);
        await db.SaveChangesAsync(ct);
        return meeting.Id;
    }

    public async Task<IReadOnlyList<CommitteeMeetingDto>> GetAllAsync(long? committeeId, CancellationToken ct = default)
    {
        var meetings = db.QualityCommitteeMeetings.AsNoTracking()
            .Include(m => m.Committee).Include(m => m.ActionItems).ThenInclude(a => a.OwnerUser)
            .AsQueryable();
        if (committeeId is { } id) meetings = meetings.Where(m => m.CommitteeId == id);
        var rows = await meetings.OrderByDescending(m => m.MeetingDate).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task CompleteActionItemAsync(long meetingId, long actionItemId, CancellationToken ct = default)
    {
        var item = await db.QualityCommitteeActionItems.FirstOrDefaultAsync(a => a.Id == actionItemId && a.MeetingId == meetingId, ct)
            ?? throw new NotFoundException(nameof(QualityCommitteeActionItem), actionItemId);
        item.IsCompleted = true;
        item.CompletedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    private static CommitteeMeetingDto ToDto(QualityCommitteeMeeting m) => new(
        m.Id, m.CommitteeId, m.Committee!.Name, m.MeetingDate, m.Minutes, m.AttendeesText,
        m.ActionItems.Select(a => new CommitteeActionItemDto(a.Id, a.Description, a.OwnerUser!.FullName, a.DueDate, a.IsCompleted, a.CompletedAt)).ToList());

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
