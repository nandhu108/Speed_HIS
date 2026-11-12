namespace Prixo.Domain.Enums;

/// <summary>
/// What a Store holds (plan §13: "store-type separation"). Pharmacy is the default for
/// every store created before this phase (migration backfills existing rows); the rest are
/// the general-stores types the same engine now also serves.
/// </summary>
public enum StoreType
{
    Pharmacy = 1,
    General = 2,
    Linen = 3,
    Stationery = 4,
    Consumables = 5,
    BiomedicalSpares = 6,
}
