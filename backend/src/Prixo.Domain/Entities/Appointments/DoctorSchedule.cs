using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Appointments;

/// <summary>
/// Weekly recurring availability block for a doctor at a branch (e.g. "Mon 09:00-13:00,
/// 10-minute slots"). A doctor may have several blocks per day (morning/evening sessions).
/// </summary>
public class DoctorSchedule : BranchEntity
{
    public long DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotMinutes { get; set; }
}
