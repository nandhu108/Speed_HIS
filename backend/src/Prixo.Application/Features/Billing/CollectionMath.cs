using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Billing;

namespace Prixo.Application.Features.Billing;

/// <summary>
/// One definition of "money the counter should hold", shared by Day Close and Cashier Shift:
/// bill payments + deposits (advances) received, minus cash refunds paid out. Refunds are
/// netted against the CASH payment mode, since a cash refund leaves the drawer as cash.
/// </summary>
public static class CollectionMath
{
    public static async Task<IReadOnlyList<DailyCollectionModeDto>> ComputeAsync(
        IAppDbContext db, IQueryable<BillPayment> payments, IQueryable<Advance> advances, IQueryable<Refund> cashRefunds,
        CancellationToken ct)
    {
        var fromPayments = await payments.AsNoTracking()
            .GroupBy(p => p.PaymentModeId)
            .Select(g => new { ModeId = g.Key, Amount = g.Sum(p => p.Amount), Count = g.Count() })
            .ToListAsync(ct);
        var fromAdvances = await advances.AsNoTracking()
            .GroupBy(a => a.PaymentModeId)
            .Select(g => new { ModeId = g.Key, Amount = g.Sum(a => a.Amount), Count = g.Count() })
            .ToListAsync(ct);
        var refunded = await cashRefunds.AsNoTracking().SumAsync(r => (decimal?)r.Amount, ct) ?? 0;
        var refundCount = refunded > 0 ? await cashRefunds.AsNoTracking().CountAsync(ct) : 0;

        var totals = new Dictionary<long, (decimal Amount, int Count)>();
        foreach (var row in fromPayments.Concat(fromAdvances))
        {
            var cur = totals.GetValueOrDefault(row.ModeId);
            totals[row.ModeId] = (cur.Amount + row.Amount, cur.Count + row.Count);
        }

        var modes = await db.PaymentModes.AsNoTracking().ToDictionaryAsync(m => m.Id, ct);
        if (refunded > 0)
        {
            var cash = modes.Values.FirstOrDefault(m => m.Code == "CASH");
            if (cash is not null)
            {
                var cur = totals.GetValueOrDefault(cash.Id);
                totals[cash.Id] = (cur.Amount - refunded, cur.Count + refundCount);
            }
        }

        return totals
            .Select(t => new DailyCollectionModeDto(t.Key, modes.TryGetValue(t.Key, out var m) ? m.Name : $"Mode {t.Key}", t.Value.Amount, t.Value.Count))
            .OrderBy(r => r.PaymentModeName)
            .ToList();
    }
}
