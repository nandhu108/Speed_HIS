using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Masters;

/// <summary>
/// Branch price list with an effective window. Rate resolution picks the newest
/// effective tariff for (branch, patient category, date); items map service → price.
/// Rate revision = a new tariff version with a later EffectiveFrom (plan §6 Billing).
/// </summary>
public class Tariff : BranchEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public long? PatientCategoryId { get; set; }
    public PatientCategory? PatientCategory { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public ICollection<TariffItem> Items { get; set; } = [];
}

public class TariffItem : BaseEntity
{
    public long TariffId { get; set; }
    public Tariff? Tariff { get; set; }
    public long ServiceId { get; set; }
    public Service? Service { get; set; }
    /// <summary>Null prices the service the same regardless of the patient's room; set, it only applies while the patient is (or the estimate is priced for) that room type — same match-or-null-fallback shape as Tariff.PatientCategoryId, just scoped to the item instead of the whole tariff, since one tariff can mix room-dependent and room-independent services.</summary>
    public long? RoomTypeId { get; set; }
    public RoomType? RoomType { get; set; }
    public decimal Rate { get; set; }
}
