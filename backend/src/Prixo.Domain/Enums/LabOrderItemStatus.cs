namespace Prixo.Domain.Enums;

/// <summary>
/// The real per-test pipeline. No distinct "Rejected" state here — a rejected sample
/// bounces every item that used it back to Ordered awaiting recollection; the rejection
/// itself is recorded on Sample.
/// </summary>
public enum LabOrderItemStatus
{
    Ordered = 1,
    Collected = 2,
    Received = 3,
    ResultEntered = 4,
    Verified = 5,
    Approved = 6,
    Cancelled = 7,
}
