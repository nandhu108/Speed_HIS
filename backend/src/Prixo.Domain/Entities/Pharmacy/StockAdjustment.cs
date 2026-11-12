using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Pharmacy;

/// <summary>Reason-coded stock correction — the one true approval-gated workflow in this phase, matching Billing's discount-approval posture.</summary>
public class StockAdjustment : BranchEntity
{
    public long StoreId { get; set; }
    public Store? Store { get; set; }
    public required string AdjustmentNumber { get; set; }
    public DateOnly AdjustmentDate { get; set; }
    public required string Reason { get; set; }
    public StockAdjustmentStatus Status { get; set; } = StockAdjustmentStatus.Pending;
    public long? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? RejectionReason { get; set; }

    public ICollection<StockAdjustmentItem> Items { get; set; } = [];
}

public class StockAdjustmentItem : BaseEntity
{
    public long StockAdjustmentId { get; set; }
    public StockAdjustment? StockAdjustment { get; set; }
    public long BatchId { get; set; }
    public Batch? Batch { get; set; }
    /// <summary>Signed — positive increases stock, negative decreases it.</summary>
    public decimal QuantityChange { get; set; }
}
