using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Reports;

namespace Prixo.Application.Features.Reports;

/// <summary>
/// Report subscription placeholder (plan §23: "schedule + outbox") — see ReportSubscription's
/// doc comment. This service only manages the intent rows (create/list/delete); nothing ever
/// reads CronExpression and actually dispatches a report, since no mailer exists yet. Scoped
/// to "my own subscriptions" — a user manages their own report deliveries, not anyone else's.
/// </summary>
public class ReportSubscriptionService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<ReportSubscriptionDto>> GetMineAsync(CancellationToken ct = default) =>
        await db.ReportSubscriptions.AsNoTracking().Where(s => s.UserId == currentUser.UserId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ReportSubscriptionDto(s.Id, s.ReportCode, s.CronExpression, s.RecipientEmail, s.IsActive, s.CreatedAt))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(CreateReportSubscriptionRequest request, CancellationToken ct = default)
    {
        var subscription = new ReportSubscription
        {
            UserId = currentUser.UserId, ReportCode = request.ReportCode, CronExpression = request.CronExpression,
            RecipientEmail = request.RecipientEmail, CreatedAt = timeProvider.GetUtcNow(),
        };
        db.ReportSubscriptions.Add(subscription);
        await db.SaveChangesAsync(ct);
        return subscription.Id;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var subscription = await db.ReportSubscriptions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException(nameof(ReportSubscription), id);
        db.ReportSubscriptions.Remove(subscription);
        await db.SaveChangesAsync(ct);
    }
}
