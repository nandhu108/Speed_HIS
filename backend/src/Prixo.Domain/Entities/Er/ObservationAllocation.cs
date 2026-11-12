using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Er;

/// <summary>
/// Short-stay observation bed allocation (plan §16's "Observation Ward") — same shape as
/// IPD's BedAllocation but keyed to an EmergencyVisit rather than an Admission, since an
/// observation stay is deliberately lighter-weight than a formal IPD admission. Allocated
/// from Ward.IsObservation-flagged wards.
/// </summary>
public class ObservationAllocation : BaseEntity
{
    public long EmergencyVisitId { get; set; }
    public EmergencyVisit? EmergencyVisit { get; set; }
    public long BedId { get; set; }
    public Bed? Bed { get; set; }

    public DateTimeOffset AllocatedAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
}
