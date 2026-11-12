namespace Prixo.Domain.Enums;

/// <summary>Order-level rollup, recomputed from item statuses — mirrors the Bill totals-recompute pattern.</summary>
public enum LabOrderStatus
{
    Ordered = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
}
