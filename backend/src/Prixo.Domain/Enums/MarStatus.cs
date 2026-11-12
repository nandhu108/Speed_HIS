namespace Prixo.Domain.Enums;

/// <summary>Doubles as both the MarSchedule row status and the MarAdministration action taken — Pending only ever appears on the schedule side.</summary>
public enum MarStatus
{
    Pending = 1,
    Administered = 2,
    Skipped = 3,
    Refused = 4,
}
