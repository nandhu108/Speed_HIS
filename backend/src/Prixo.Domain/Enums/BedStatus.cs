namespace Prixo.Domain.Enums;

/// <summary>
/// Occupied is driven exclusively by an active BedAllocation (never set directly); the
/// other four are toggled manually from the bed board.
/// </summary>
public enum BedStatus
{
    Vacant = 1,
    Occupied = 2,
    Reserved = 3,
    Cleaning = 4,
    Blocked = 5,
}
