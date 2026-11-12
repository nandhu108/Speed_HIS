namespace Prixo.Domain.Enums;

/// <summary>
/// Ordered is the only status this phase drives end-to-end (no Lab/Radiology worklist
/// exists yet to progress it). InProgress/Completed are set by those modules later.
/// </summary>
public enum ClinicalOrderStatus
{
    Ordered = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
}
