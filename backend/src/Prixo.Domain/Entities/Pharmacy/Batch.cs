using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Pharmacy;

/// <summary>
/// One purchased lot of a drug OR a general item (Phase 13: "reuses the pharmacy inventory
/// engine") in a store — exactly one of DrugId/ItemId is set, enforced in the service layer,
/// not by a DB check constraint (same "invariant lives in code, not SQL" posture as
/// RadOrderItem's exactly-one-of AccessionNumber path). Created/topped-up by posting a GRN.
/// QuantityOnHand is a denormalized running balance for fast stock lookups — the
/// StockLedger is the append-only source of truth it's reconstructable from. ExpiryDate/Mrp
/// are nullable because general items (stationery, linen, biomedical spares) often have
/// neither a meaningful expiry nor an MRP.
/// </summary>
public class Batch : BranchEntity
{
    public long? DrugId { get; set; }
    public Drug? Drug { get; set; }
    public long? ItemId { get; set; }
    public Item? Item { get; set; }
    public long StoreId { get; set; }
    public Store? Store { get; set; }
    public required string BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal? Mrp { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal QuantityOnHand { get; set; }
}
