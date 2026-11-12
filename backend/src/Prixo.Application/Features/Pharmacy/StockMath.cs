using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Pharmacy;

/// <summary>A batch candidate for FEFO allocation — no EF/tracking baggage.</summary>
public record FefoBatchCandidate(long BatchId, DateOnly ExpiryDate, decimal Available);

public record FefoAllocation(long BatchId, decimal Quantity);

/// <summary>
/// Pure stock math — FEFO allocation, running-balance direction, and sale/issue line
/// amounts — no I/O, so the pharmacy services delegate here and the rules stay
/// unit-testable without a database. Same reasoning as Billing's BillMath / Lab's LabMath.
/// </summary>
public static class StockMath
{
    private static readonly HashSet<StockMovementType> InboundTypes =
    [
        StockMovementType.PurchaseIn,
        StockMovementType.SaleReturnIn,
        StockMovementType.IssueReturnIn,
        StockMovementType.AdjustmentIn,
        StockMovementType.TransferIn,
    ];

    public static bool IsInbound(StockMovementType type) => InboundTypes.Contains(type);

    public static decimal ApplyMovement(decimal currentBalance, StockMovementType type, decimal quantity) =>
        IsInbound(type) ? currentBalance + quantity : currentBalance - quantity;

    /// <summary>
    /// Earliest-expiry-first allocation across candidates, already assumed sorted however
    /// the caller likes (this re-sorts by ExpiryDate to be safe). Returns however much it
    /// could allocate — the caller decides whether a shortfall (Remaining &gt; 0) is an error.
    /// </summary>
    public static (IReadOnlyList<FefoAllocation> Allocations, decimal Remaining) AllocateFefo(
        IEnumerable<FefoBatchCandidate> candidates, decimal requiredQuantity)
    {
        var allocations = new List<FefoAllocation>();
        var remaining = requiredQuantity;
        foreach (var candidate in candidates.OrderBy(c => c.ExpiryDate))
        {
            if (remaining <= 0)
            {
                break;
            }
            var take = Math.Min(candidate.Available, remaining);
            if (take <= 0)
            {
                continue;
            }
            allocations.Add(new FefoAllocation(candidate.BatchId, take));
            remaining -= take;
        }
        return (allocations, remaining);
    }

    /// <summary>
    /// Retail (MRP) line amounts. In India MRP is the maximum retail price INCLUSIVE of all taxes, so
    /// the patient pays at most quantity × MRP: gross = MRP × qty, less discount, and the GST is the
    /// part of that (tax-inclusive) amount — never added on top.
    /// </summary>
    public static (decimal DiscountAmount, decimal TaxAmount, decimal TotalAmount) ComputeMrpInclusiveLineAmounts(
        decimal mrp, decimal quantity, decimal discountPercent, decimal taxPercent)
    {
        var gross = mrp * quantity;
        var discountAmount = Math.Round(gross * discountPercent / 100, 2);
        var total = gross - discountAmount;
        var taxAmount = taxPercent <= 0 ? 0 : Math.Round(total - total / (1 + taxPercent / 100), 2);
        return (discountAmount, taxAmount, total);
    }

    /// <summary>A batch is saleable/issuable only while it has not expired (expiry date strictly after today).</summary>
    public static bool IsUsable(DateOnly? expiryDate, DateOnly today) => expiryDate is null || expiryDate.Value > today;

    /// <summary>Gross → discount → taxable → tax → total, same order of operations as Billing's ComputeItemAmounts.</summary>
    public static (decimal DiscountAmount, decimal TaxAmount, decimal TotalAmount) ComputeLineAmounts(
        decimal rate, decimal quantity, decimal discountPercent, decimal taxPercent)
    {
        var gross = rate * quantity;
        var discountAmount = Math.Round(gross * discountPercent / 100, 2);
        var taxable = gross - discountAmount;
        var taxAmount = Math.Round(taxable * taxPercent / 100, 2);
        return (discountAmount, taxAmount, taxable + taxAmount);
    }
}
