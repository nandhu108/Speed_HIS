using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Ot;

/// <summary>The operative note template — one row per booking, written by the operating surgeon.</summary>
public class OperativeNote : BaseEntity
{
    public long BookingId { get; set; }
    public SurgeryBooking? Booking { get; set; }

    public long SurgeonId { get; set; }
    public Doctor? Surgeon { get; set; }
    public required string ProcedurePerformed { get; set; }
    public string? Findings { get; set; }
    public string? Complications { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Post-operative / recovery note — one row per booking. Actual bed/ward transfer after surgery is IPD's existing BedTransfer flow (Phase 9); this only records the clinical handover.</summary>
public class PostOpNote : BaseEntity
{
    public long BookingId { get; set; }
    public SurgeryBooking? Booking { get; set; }

    public string? RecoveryNotes { get; set; }
    public string? VitalsSummary { get; set; }
    public string? Complications { get; set; }
    public DateTimeOffset? ShiftedAt { get; set; }
    public string? Notes { get; set; }
}
