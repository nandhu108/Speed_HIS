namespace Prixo.Domain.Enums;

/// <summary>The real per-test pipeline, mirroring Lab's LabOrderItemStatus shape with Scheduled/ImageCaptured standing in for Collected/Received.</summary>
public enum RadOrderItemStatus
{
    Ordered = 1,
    Scheduled = 2,
    ImageCaptured = 3,
    ReportEntered = 4,
    Verified = 5,
    Approved = 6,
    Cancelled = 7,
}
