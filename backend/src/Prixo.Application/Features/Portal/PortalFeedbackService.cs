using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Portal;

/// <summary>Portal Feedback (plan §21: "visit-linked ratings → Quality module"). See Feedback's doc comment for why this stays in the `patient` schema until Phase 20 exists.</summary>
public class PortalFeedbackService(IAppDbContext db, IPortalCurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider)
{
    public async Task<long> SubmitAsync(SubmitFeedbackRequest request, CancellationToken ct = default)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new DomainException("Rating must be between 1 and 5.");
        }
        if (request.OpVisitId is null && request.AdmissionId is null)
        {
            throw new DomainException("Feedback must be linked to a visit or an admission.");
        }

        if (request.OpVisitId is { } opVisitId && !await db.OpVisits.AnyAsync(v => v.Id == opVisitId && v.PatientId == currentUser.PatientId, ct))
        {
            throw new DomainException("That visit does not belong to you.");
        }
        if (request.AdmissionId is { } admissionId && !await db.Admissions.AnyAsync(a => a.Id == admissionId && a.PatientId == currentUser.PatientId, ct))
        {
            throw new DomainException("That admission does not belong to you.");
        }

        var feedback = new Feedback
        {
            PatientId = currentUser.PatientId, OpVisitId = request.OpVisitId, AdmissionId = request.AdmissionId,
            Rating = request.Rating, Comments = request.Comments, SubmittedAt = timeProvider.GetUtcNow(),
        };
        db.Feedbacks.Add(feedback);
        await activity.LogAsync(ActivityEvents.PortalFeedbackSubmitted, $"Feedback submitted (rating {request.Rating})", ct: ct);
        await db.SaveChangesAsync(ct);
        return feedback.Id;
    }

    public async Task<IReadOnlyList<FeedbackDto>> GetMineAsync(CancellationToken ct = default) =>
        await db.Feedbacks.AsNoTracking().Where(f => f.PatientId == currentUser.PatientId)
            .OrderByDescending(f => f.SubmittedAt)
            .Select(f => new FeedbackDto(f.Id, f.OpVisitId, f.AdmissionId, f.Rating, f.Comments, f.SubmittedAt))
            .ToListAsync(ct);
}
