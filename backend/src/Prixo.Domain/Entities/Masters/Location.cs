using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Masters;

// Physical hierarchy: building → floor → ward → room → bed. All branch-scoped
// operational masters (plan §10). Room type and ward link drive bed charges later.

public class Building : BranchEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
}

public class Floor : BranchEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public long BuildingId { get; set; }
    public Building? Building { get; set; }
}

/// <summary>Room class (General, Semi-Private, Deluxe…) with the default daily bed charge — BedChargePerDay is the fallback rate used when no RoomTypeCharge row is effective as of the allocation date (see RoomTypeCharge, IpdMath.ResolveRoomTypeCharge).</summary>
public class RoomType : BranchEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public decimal BedChargePerDay { get; set; }

    public ICollection<RoomTypeCharge> Charges { get; set; } = [];
}

/// <summary>
/// A date-effective daily bed-charge revision for a RoomType — same shape/philosophy as
/// Tariff (a rate revision is a new row with a later EffectiveFrom, old rows kept for history),
/// scoped to one room type instead of a whole branch price list since room charges revise
/// independently of service tariffs. AdmissionService snapshots the resolved rate onto
/// BedAllocation.BedChargePerDay at allocation/transfer time, same as it already did against
/// the old flat RoomType.BedChargePerDay — a later rate change never rewrites an in-progress
/// stay's history.
/// </summary>
public class RoomTypeCharge : BranchEntity
{
    public long RoomTypeId { get; set; }
    public RoomType? RoomType { get; set; }
    public decimal Rate { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

public class Ward : BranchEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public long FloorId { get; set; }
    public Floor? Floor { get; set; }
    /// <summary>Marks ICU-capable wards (Phase 15 allocates ICU stays to these).</summary>
    public bool IsIcu { get; set; }
    /// <summary>Marks short-stay observation wards (plan §16 — ER's ObservationAllocation allocates its beds from these).</summary>
    public bool IsObservation { get; set; }
}

public class Room : BranchEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public long WardId { get; set; }
    public Ward? Ward { get; set; }
    public long RoomTypeId { get; set; }
    public RoomType? RoomType { get; set; }
}

/// <summary>
/// Physical bed. Status is mutable operational state (plan §9's bed board) rather than
/// derived-only: Occupied is set exclusively by Phase 9's BedAllocation logic, but the other
/// four values (Vacant/Reserved/Cleaning/Blocked) are toggled directly from the bed board —
/// there's no admission to derive them from.
/// </summary>
public class Bed : BranchEntity
{
    public required string Code { get; set; }
    public long RoomId { get; set; }
    public Room? Room { get; set; }
    public BedStatus Status { get; set; } = BedStatus.Vacant;
}
