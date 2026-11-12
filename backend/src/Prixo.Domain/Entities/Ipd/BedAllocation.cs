using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Ipd;

/// <summary>
/// One row per bed the admission has occupied. ReleasedAt null means this is the current
/// bed — the DB enforces at most one such row per bed AND per admission via partial unique
/// indexes (IpdConfigurations), which is what makes allocation concurrency-safe: a second
/// concurrent allocation to the same bed hits the index and fails instead of double-booking.
/// BedChargePerDay snapshots RoomType's rate at allocation time so a later tariff change
/// doesn't rewrite an in-progress stay's history — same snapshot-on-write idiom as GRN's
/// Mrp/PurchaseRate.
/// </summary>
public class BedAllocation : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public long BedId { get; set; }
    public Bed? Bed { get; set; }

    public decimal BedChargePerDay { get; set; }
    public DateTimeOffset AllocatedAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public string? TransferReason { get; set; }
}
