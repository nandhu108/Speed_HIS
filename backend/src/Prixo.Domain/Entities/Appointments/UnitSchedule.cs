using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Appointments;

/// <summary>
/// Weekly recurring operating-hours block for a Unit ("Schedule Unit - Day Wise"), mirrors
/// DoctorSchedule minus SlotMinutes — purely descriptive, not consumed by the appointment
/// slot engine (nothing links Unit to Appointment/booking).
/// </summary>
public class UnitSchedule : BranchEntity
{
    public long UnitId { get; set; }
    public Unit? Unit { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
