using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Pharmacy;

/// <summary>Physical count sheet; completing it posts an adjustment StockLedger row per variance — same declared-vs-system idiom as Billing's DayClose.</summary>
public class StockAudit : BranchEntity
{
    public long StoreId { get; set; }
    public Store? Store { get; set; }
    public required string AuditNumber { get; set; }
    public DateOnly AuditDate { get; set; }
    public StockAuditStatus Status { get; set; } = StockAuditStatus.InProgress;
    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<StockAuditItem> Items { get; set; } = [];
}

public class StockAuditItem : BaseEntity
{
    public long StockAuditId { get; set; }
    public StockAudit? StockAudit { get; set; }
    public long BatchId { get; set; }
    public Batch? Batch { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal Variance { get; set; }
}
