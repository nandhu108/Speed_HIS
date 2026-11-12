using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Rad;

/// <summary>
/// Appointment/slot scheduling per modality (plan §12) — one row per RadOrderItem once
/// scheduled. ModalityId is denormalized from RadiologyTest.ModalityId at scheduling time so
/// the technician worklist can filter by modality without an extra join, same snapshot idiom
/// used across this codebase (e.g. BedAllocation.BedChargePerDay).
/// </summary>
public class RadSchedule : BaseEntity
{
    public long RadOrderItemId { get; set; }
    public RadOrderItem? RadOrderItem { get; set; }
    public long ModalityId { get; set; }
    public Modality? Modality { get; set; }

    public DateTimeOffset ScheduledAt { get; set; }
    public string? Notes { get; set; }
    public long ScheduledByUserId { get; set; }
    public User? ScheduledByUser { get; set; }
}
