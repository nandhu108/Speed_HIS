using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Billing;

/// <summary>A tariff row candidate for rate resolution — no EF/tracking baggage, just the fields the match needs.</summary>
public record TariffCandidate(long? PatientCategoryId, DateOnly EffectiveFrom, DateOnly? EffectiveTo, decimal Rate, long? RoomTypeId = null);

/// <summary>An advance's remaining balance for the auto-drawdown sweep — no EF/tracking baggage.</summary>
public record AdvanceBalance(long AdvanceId, decimal BalanceAmount);

/// <summary>One advance's contribution to a bill, as decided by <see cref="BillMath.PlanAdvanceSweep"/>.</summary>
public record AdvanceDraw(long AdvanceId, decimal Amount);

/// <summary>
/// Pure bill/item amount math and discount/tariff selection rules — no I/O, so BillingService
/// delegates here and the rules stay unit-testable without a database.
/// </summary>
public static class BillMath
{
    public static void ComputeItemAmounts(BillItem item)
    {
        var gross = item.Rate * item.Quantity;
        item.DiscountAmount = Math.Round(gross * item.DiscountPercent / 100, 2);
        var taxable = gross - item.DiscountAmount;
        item.TaxAmount = Math.Round(taxable * item.TaxPercent / 100, 2);
        item.TotalAmount = taxable + item.TaxAmount;
    }

    public static void ApplyItemDiscountPercent(BillItem item, decimal percent)
    {
        item.DiscountPercent = percent;
        ComputeItemAmounts(item);
    }

    public static void RecomputeBillTotals(Bill bill)
    {
        bill.SubTotal = bill.Items.Sum(i => i.Rate * i.Quantity);
        var itemDiscountTotal = bill.Items.Sum(i => i.DiscountAmount);
        var taxTotal = bill.Items.Sum(i => i.TaxAmount);
        var baseAfterItemDiscount = bill.SubTotal - itemDiscountTotal;
        var billLevelDiscountAmount = Math.Round(baseAfterItemDiscount * bill.BillDiscountPercent / 100, 2);

        bill.DiscountAmount = itemDiscountTotal + billLevelDiscountAmount;
        bill.TaxAmount = taxTotal;
        bill.TotalAmount = bill.SubTotal - bill.DiscountAmount + bill.TaxAmount;
        bill.BalanceAmount = Math.Max(bill.TotalAmount - bill.PaidAmount, 0);
        if (bill.PaidAmount > 0)
        {
            bill.Status = bill.BalanceAmount <= 0 ? BillStatus.Paid : BillStatus.PartiallyPaid;
        }
    }

    /// <summary>No DiscountTypeId (threshold 0) means an ad-hoc discount, which always requires approval.</summary>
    public static bool RequiresApproval(decimal percent, decimal approvalThresholdPercent) => percent > approvalThresholdPercent;

    /// <summary>
    /// Prefers the candidate matching the most dimensions (patient category, room type) over a
    /// more generic one, then the most recently effective row; falls back to the service's
    /// default rate if nothing matches. A candidate whose PatientCategoryId/RoomTypeId is set
    /// only matches a request carrying that same value — null on either side means "applies
    /// regardless" for that one dimension, not "matches everything."
    /// </summary>
    public static decimal ResolveTariffRate(
        IEnumerable<TariffCandidate> candidates, long? patientCategoryId, DateOnly asOf, decimal fallbackRate, long? roomTypeId = null)
    {
        var match = candidates
            .Where(c => c.EffectiveFrom <= asOf && (c.EffectiveTo == null || c.EffectiveTo >= asOf)
                && (c.PatientCategoryId == null || c.PatientCategoryId == patientCategoryId)
                && (c.RoomTypeId == null || c.RoomTypeId == roomTypeId))
            .OrderByDescending(c => (c.PatientCategoryId != null ? 1 : 0) + (c.RoomTypeId != null ? 1 : 0))
            .ThenByDescending(c => c.EffectiveFrom)
            .FirstOrDefault();
        return match?.Rate ?? fallbackRate;
    }

    /// <summary>
    /// Plans an auto-drawdown sweep of a bill's outstanding balance against a set of available
    /// advances, oldest first (caller supplies FIFO order, e.g. by ReceivedAt). Stops as soon as
    /// the balance is covered; an advance with a zero/negative balance contributes nothing.
    /// Returns only the non-zero draws — BillingService applies each one to the tracked
    /// Advance/Bill entities and recomputes PaidAmount/BalanceAmount/Status itself.
    /// </summary>
    public static IReadOnlyList<AdvanceDraw> PlanAdvanceSweep(IEnumerable<AdvanceBalance> advancesFifoOrder, decimal billBalance)
    {
        var draws = new List<AdvanceDraw>();
        var remaining = billBalance;
        foreach (var advance in advancesFifoOrder)
        {
            if (remaining <= 0)
            {
                break;
            }
            var amount = Math.Min(advance.BalanceAmount, remaining);
            if (amount > 0)
            {
                draws.Add(new AdvanceDraw(advance.AdvanceId, amount));
                remaining -= amount;
            }
        }
        return draws;
    }
}
