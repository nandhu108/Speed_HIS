using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Pharmacy;

/// <summary>
/// Store↔store (same branch) or branch↔branch transfer. BranchId (from BranchEntity) is
/// the source branch; ToBranchId defaults to the same branch for a same-branch,
/// store-to-store move.
/// </summary>
public class StockTransfer : BranchEntity
{
    public long FromStoreId { get; set; }
    public Store? FromStore { get; set; }
    public long ToBranchId { get; set; }
    public Branch? ToBranch { get; set; }
    public long ToStoreId { get; set; }
    public Store? ToStore { get; set; }
    public required string TransferNumber { get; set; }
    public DateOnly TransferDate { get; set; }
    public StockTransferStatus Status { get; set; } = StockTransferStatus.Pending;
    public DateTimeOffset? DispatchedAt { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public string? Notes { get; set; }

    public ICollection<StockTransferItem> Items { get; set; } = [];
}

public class StockTransferItem : BaseEntity
{
    public long StockTransferId { get; set; }
    public StockTransfer? StockTransfer { get; set; }
    public long BatchId { get; set; }
    public Batch? Batch { get; set; }
    public decimal Quantity { get; set; }
    /// <summary>Set once received — the batch created/topped-up at the destination store.</summary>
    public long? ReceivedBatchId { get; set; }
    public Batch? ReceivedBatch { get; set; }
}
