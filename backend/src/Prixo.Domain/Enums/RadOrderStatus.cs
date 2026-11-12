namespace Prixo.Domain.Enums;

/// <summary>Order-level rollup, recomputed from item statuses — mirrors Lab's LabOrderStatus.</summary>
public enum RadOrderStatus
{
    Ordered = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
}
