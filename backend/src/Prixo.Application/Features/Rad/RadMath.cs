using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Rad;

/// <summary>
/// Pure order-status rollup — no I/O, so RadOrderService delegates here and the rule stays
/// unit-testable without a database. Same reasoning as Lab's LabMath.RecomputeOrderStatus.
/// </summary>
public static class RadMath
{
    /// <summary>
    /// Order-level rollup from item statuses — same recompute-from-children idiom as
    /// Lab's LabMath.RecomputeOrderStatus / Billing's RecomputeBillTotals. Cancelled items
    /// are excluded from the all-approved/all-cancelled checks except when literally every
    /// item is cancelled.
    /// </summary>
    public static RadOrderStatus RecomputeOrderStatus(IReadOnlyCollection<RadOrderItemStatus> itemStatuses)
    {
        if (itemStatuses.Count == 0 || itemStatuses.All(s => s == RadOrderItemStatus.Cancelled))
        {
            return RadOrderStatus.Cancelled;
        }
        var relevant = itemStatuses.Where(s => s != RadOrderItemStatus.Cancelled).ToList();
        if (relevant.All(s => s == RadOrderItemStatus.Approved))
        {
            return RadOrderStatus.Completed;
        }
        return itemStatuses.Any(s => s != RadOrderItemStatus.Ordered)
            ? RadOrderStatus.InProgress
            : RadOrderStatus.Ordered;
    }
}
