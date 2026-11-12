namespace Prixo.Domain.Enums;

public enum DiscountApprovalStatus
{
    /// <summary>Below the discount type's threshold — applied immediately, no row needed at all.
    /// Kept for completeness on rows that do get created (at/above threshold).</summary>
    AutoApproved = 1,
    Pending = 2,
    Approved = 3,
    Rejected = 4,
}
