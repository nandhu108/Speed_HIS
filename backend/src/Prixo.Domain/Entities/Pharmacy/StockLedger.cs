using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Pharmacy;

/// <summary>
/// Append-only stock movement log — never updated or deleted (plan §7: "every stock
/// movement writes an immutable stock_ledger row, running balance reconstructable").
/// Quantity is always positive; MovementType's In/Out suffix says which direction.
/// ReferenceType/ReferenceId point at the document that caused the movement (Grn,
/// PurchaseReturn, PharmacySale, SalesReturn, Issue, IssueReturn, StockAdjustment,
/// StockTransfer) — a string discriminator rather than N nullable FKs, since exactly one
/// document type ever caused any given row.
/// </summary>
public class StockLedger : BranchEntity
{
    public long StoreId { get; set; }
    public Store? Store { get; set; }
    public long BatchId { get; set; }
    public Batch? Batch { get; set; }
    public StockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal RunningBalance { get; set; }
    public required string ReferenceType { get; set; }
    public long ReferenceId { get; set; }
    public DateTimeOffset MovementAt { get; set; }
    public string? Notes { get; set; }
}
