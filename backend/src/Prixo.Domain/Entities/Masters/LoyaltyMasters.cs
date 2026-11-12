using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Masters;

// Loyalty card program masters (front-office extension — see docs/modules/loyalty.md).
// Deliberately separate from the pre-existing LoyaltyProgram (FrontOfficeMasters.cs),
// which stays untouched/unused rather than being overloaded with this batch's meaning.

/// <summary>Loyalty card category tag, e.g. Individual/Family/Corporate.</summary>
public class LoyaltyCardCategory : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>Member relation tag for dependents, e.g. Spouse/Child/Parent.</summary>
public class MemberRelationTag : BaseEntity, ISimpleMaster
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>Loyalty card product — not ISimpleMaster since it carries MemberLimit + an
/// optional category FK beyond Code/Name/Description, same reasoning as DoctorPayoutRule.</summary>
public class LoyaltyCardType : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    /// <summary>Max number of active dependents a card of this type may carry ("Member Limit Fix").</summary>
    public int MemberLimit { get; set; }
    public long? LoyaltyCardCategoryId { get; set; }
    public LoyaltyCardCategory? LoyaltyCardCategory { get; set; }
}

/// <summary>
/// Branch price list for a card type with an effective window, mirroring Tariff — the
/// "Loyalty Card Price Matrix": Type x effective window x duration (Items) -> price.
/// </summary>
public class LoyaltyCardPrice : BranchEntity
{
    public long LoyaltyCardTypeId { get; set; }
    public LoyaltyCardType? LoyaltyCardType { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public ICollection<LoyaltyCardPriceItem> Items { get; set; } = [];
}

public class LoyaltyCardPriceItem : BaseEntity
{
    public long LoyaltyCardPriceId { get; set; }
    public LoyaltyCardPrice? LoyaltyCardPrice { get; set; }
    public int DurationMonths { get; set; }
    public decimal Amount { get; set; }
}
