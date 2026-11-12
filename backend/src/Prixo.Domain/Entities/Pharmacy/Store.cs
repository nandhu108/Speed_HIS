using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Pharmacy;

/// <summary>
/// A stock-holding location within a branch (main pharmacy store, OT store, ward
/// store, linen store…). Every Batch/StockLedger row belongs to exactly one store.
/// StoreType (Phase 13) is purely descriptive — it doesn't gate which Batch.DrugId/ItemId
/// combination a store may hold; nothing stops a General store from also carrying a drug
/// batch if a hospital genuinely wants that.
/// </summary>
public class Store : BranchEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsDefault { get; set; }
    public StoreType StoreType { get; set; } = StoreType.Pharmacy;
}

/// <summary>Storage rack/shelf within a store — informational only, not stock-tracked at rack granularity.</summary>
public class Rack : BranchEntity
{
    public long StoreId { get; set; }
    public Store? Store { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
}
