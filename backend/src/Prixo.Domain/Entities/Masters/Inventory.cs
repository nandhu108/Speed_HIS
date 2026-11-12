using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Masters;

// Store/pharmacy reference masters. Catalogs are global (plan §10); stock itself
// is branch-scoped and lands with the Phase 8 stock engine.

public class Vendor : BranchEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? GstNumber { get; set; }
}

public class ItemCategory : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>General-store item (consumables, linen, stationery…). Global catalog.</summary>
public class Item : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public long ItemCategoryId { get; set; }
    public ItemCategory? Category { get; set; }
    public long UomId { get; set; }
    public Uom? Uom { get; set; }
    public string? HsnCode { get; set; }
    public long? TaxId { get; set; }
    public Tax? Tax { get; set; }
    public decimal ReorderLevel { get; set; }
}

public class Generic : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class Manufacturer : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Drug catalog entry. Global; deep pharmacy attributes (schedule flags, racks,
/// batches) arrive with Phase 8 — the columns that need no re-architecture are here now.
/// </summary>
public class Drug : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public long? GenericId { get; set; }
    public Generic? Generic { get; set; }
    public long? ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }
    /// <summary>Dosage form: tablet, syrup, injection…</summary>
    public string? Form { get; set; }
    public string? Strength { get; set; }
    public long UomId { get; set; }
    public Uom? Uom { get; set; }
    public string? HsnCode { get; set; }
    public long? TaxId { get; set; }
    public Tax? Tax { get; set; }
    /// <summary>H / H1 / X restricted-schedule flag (placeholder until Phase 8 enforces dispensing rules).</summary>
    public string? Schedule { get; set; }
    /// <summary>Branch-agnostic default minimum-stock threshold read by the Phase 8 low-stock alert.</summary>
    public decimal ReorderLevel { get; set; }
    /// <summary>Drives Phase 10's MAR double-verification requirement on administration.</summary>
    public bool IsHighAlert { get; set; }
}
