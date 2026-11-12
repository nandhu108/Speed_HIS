namespace Prixo.Domain.Enums;

public enum PurchaseRequestStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4,
    /// <summary>A Purchase Order has been raised from this request.</summary>
    Converted = 5,
}
