using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Appointments;

/// <summary>
/// Date-specific exception to the weekly template: a full-day leave/block
/// (IsAvailable = false) or special hours for that date (IsAvailable = true with its
/// own Start/EndTime, replacing the weekly template for that day only).
/// </summary>
public class ScheduleOverride : BranchEntity
{
    public long DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public DateOnly Date { get; set; }
    public bool IsAvailable { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? SlotMinutes { get; set; }
    public string? Reason { get; set; }
}
