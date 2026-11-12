namespace Prixo.Domain.Enums;

public enum BillEstimateStatus
{
    Draft = 1,
    /// <summary>Set the first time the estimate PDF is generated — informational only, never blocks further edits.</summary>
    Printed = 2,
}
