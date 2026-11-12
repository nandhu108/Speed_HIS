using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Billing;

/// <summary>
/// The staff-side worklist over the Patient Portal's "Pay Online" outbox
/// (<see cref="OnlinePaymentRequest"/> — plan §21: payment gateway held, real schema now).
/// Closes a real gap: the portal already tells the patient "our billing team will contact
/// you," but until this existed there was no screen for staff to actually see that queue —
/// only the DB row. This service never moves money itself — payment is still collected the
/// normal way on the bill (<c>BillingService.CollectPaymentAsync</c>); Complete/Cancel here
/// just close the outbox row once staff have followed up (or the request turned out to be a
/// duplicate/mistake), so <see cref="OnlinePaymentRequestDto"/>.
/// </summary>
public class OnlinePaymentRequestService(IAppDbContext db, IActivityLogger activity, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<OnlinePaymentRequestDto>> GetAsync(OnlinePaymentRequestStatus? status, CancellationToken ct = default)
    {
        var query = db.OnlinePaymentRequests.AsNoTracking().Include(r => r.Bill).ThenInclude(b => b!.Patient).AsQueryable();
        if (status is { } s)
        {
            query = query.Where(r => r.Status == s);
        }
        return await query
            .OrderBy(r => r.Status == OnlinePaymentRequestStatus.Requested ? 0 : 1).ThenBy(r => r.RequestedAt)
            .Select(r => new OnlinePaymentRequestDto(
                r.Id, r.BillId, r.Bill!.BillNumber, r.PatientId, r.Bill.Patient!.FirstName + " " + r.Bill.Patient.LastName,
                r.Amount, r.Status, r.RequestedAt, r.ProcessedAt))
            .ToListAsync(ct);
    }

    public Task CompleteAsync(long id, CancellationToken ct = default) => DecideAsync(id, OnlinePaymentRequestStatus.Completed, ActivityEvents.OnlinePaymentRequestCompleted, "completed", ct);
    public Task CancelAsync(long id, CancellationToken ct = default) => DecideAsync(id, OnlinePaymentRequestStatus.Cancelled, ActivityEvents.OnlinePaymentRequestCancelled, "cancelled", ct);

    private async Task DecideAsync(long id, OnlinePaymentRequestStatus target, string activityEvent, string verb, CancellationToken ct)
    {
        var request = await db.OnlinePaymentRequests.Include(r => r.Bill).FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(OnlinePaymentRequest), id);
        if (request.Status != OnlinePaymentRequestStatus.Requested)
        {
            throw new DomainException($"This request is already {request.Status} and cannot be actioned again.");
        }
        request.Status = target;
        request.ProcessedAt = timeProvider.GetUtcNow();
        await activity.LogAsync(activityEvent, $"Online payment request for bill {request.Bill!.BillNumber} marked {verb}", ct: ct);
        await db.SaveChangesAsync(ct);
    }
}
