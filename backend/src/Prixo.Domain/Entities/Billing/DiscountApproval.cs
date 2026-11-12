using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Billing;

/// <summary>
/// A discount request that crossed the DiscountType's ApprovalThresholdPercent. Requests
/// below the threshold are applied immediately and never create a row here.
/// </summary>
public class DiscountApproval : BaseEntity
{
    public long BillId { get; set; }
    public Bill? Bill { get; set; }
    /// <summary>Null when the discount is bill-level rather than on one line.</summary>
    public long? BillItemId { get; set; }
    public BillItem? BillItem { get; set; }
    public long? DiscountTypeId { get; set; }
    public DiscountType? DiscountType { get; set; }

    public decimal RequestedPercent { get; set; }
    public decimal RequestedAmount { get; set; }
    public string? Reason { get; set; }
    public DiscountApprovalStatus Status { get; set; } = DiscountApprovalStatus.Pending;
    public long? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? RejectionReason { get; set; }
}
