using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Billing;

/// <summary>
/// Branch cash reconciliation: today's collection by mode, and closing the day (declared
/// vs. system amounts, variance). One close per branch per day — see DayClose doc comment
/// for why counter-level sessions aren't modeled.
/// </summary>
public class DayCloseService(IAppDbContext db, ICurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider)
{
    public async Task<DailyCollectionDto> GetDailyCollectionAsync(DateOnly date, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var rangeStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rangeEnd = rangeStart.AddDays(1);
        var rows = await CollectionMath.ComputeAsync(db,
            db.BillPayments.Where(p => p.Bill!.BranchId == branchId && p.PaidAt >= rangeStart && p.PaidAt < rangeEnd),
            db.Advances.Where(a => a.BranchId == branchId && a.ReceivedAt >= rangeStart && a.ReceivedAt < rangeEnd),
            db.Refunds.Where(r => r.Status == RefundStatus.Issued && r.Method == RefundMethod.Cash &&
                r.IssuedAt >= rangeStart && r.IssuedAt < rangeEnd &&
                ((r.Bill != null && r.Bill.BranchId == branchId) || (r.Advance != null && r.Advance.BranchId == branchId))),
            ct);

        return new DailyCollectionDto(date, rows, rows.Sum(r => r.Amount));
    }

    public async Task<DayCloseDto?> GetByDateAsync(DateOnly date, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var dayClose = await db.DayCloses.AsNoTracking()
            .Include(d => d.ModeAmounts).ThenInclude(m => m.PaymentMode)
            .FirstOrDefaultAsync(d => d.BranchId == branchId && d.Date == date, ct);
        return dayClose is null ? null : ToDto(dayClose);
    }

    public async Task<DayCloseDto> CloseDayAsync(CloseDayRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var existing = await db.DayCloses.Include(d => d.ModeAmounts)
            .FirstOrDefaultAsync(d => d.BranchId == branchId && d.Date == request.Date, ct);
        if (existing is { Status: DayCloseStatus.Closed })
        {
            throw new DomainException($"{request.Date:yyyy-MM-dd} is already closed for this branch.");
        }

        var collection = await GetDailyCollectionAsync(request.Date, ct);
        var systemByMode = collection.ByMode.ToDictionary(m => m.PaymentModeId, m => m.Amount);
        var declaredByMode = request.DeclaredAmounts.ToDictionary(d => d.PaymentModeId, d => d.DeclaredAmount);
        var modeIds = systemByMode.Keys.Union(declaredByMode.Keys).Distinct().ToList();

        var modeNames = await db.PaymentModes.AsNoTracking()
            .Where(m => modeIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.Name, ct);

        var dayClose = existing ?? new DayClose { BranchId = branchId, Date = request.Date };
        if (existing is null)
        {
            db.DayCloses.Add(dayClose);
        }
        else
        {
            db.DayCloseModeAmounts.RemoveRange(existing.ModeAmounts);
            dayClose.ModeAmounts.Clear();
        }

        foreach (var modeId in modeIds)
        {
            var systemAmount = systemByMode.GetValueOrDefault(modeId);
            var declaredAmount = declaredByMode.GetValueOrDefault(modeId);
            dayClose.ModeAmounts.Add(new DayCloseModeAmount
            {
                PaymentModeId = modeId,
                SystemAmount = systemAmount,
                DeclaredAmount = declaredAmount,
                Variance = declaredAmount - systemAmount,
            });
        }

        dayClose.Status = DayCloseStatus.Closed;
        dayClose.ClosedAt = timeProvider.GetUtcNow();
        dayClose.ClosedByUserId = currentUser.UserId;
        dayClose.Notes = request.Notes;

        await activity.LogAsync(ActivityEvents.DayClosed, $"Day close for {request.Date:yyyy-MM-dd} completed.", ct: ct);
        await db.SaveChangesAsync(ct);

        var modeAmounts = dayClose.ModeAmounts
            .Select(m => new DayCloseModeAmountDto(m.PaymentModeId, modeNames.GetValueOrDefault(m.PaymentModeId, ""),
                m.SystemAmount, m.DeclaredAmount, m.Variance))
            .ToList();
        return new DayCloseDto(dayClose.Id, dayClose.Date, dayClose.Status, dayClose.ClosedAt, null, dayClose.Notes, modeAmounts);
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");

    private static DayCloseDto ToDto(DayClose d) => new(
        d.Id, d.Date, d.Status, d.ClosedAt, null, d.Notes,
        d.ModeAmounts.Select(m => new DayCloseModeAmountDto(
            m.PaymentModeId, m.PaymentMode!.Name, m.SystemAmount, m.DeclaredAmount, m.Variance)).ToList());
}
