using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Insurance;

/// <summary>
/// Mid-treatment request to increase the approved amount on an already-approved PreAuthRequest
/// — the plan's "enhancement" stage. Same request→approve/reject shape as Billing's
/// DiscountApproval/Refund.
/// </summary>
public class PreAuthEnhancement : BaseEntity
{
    public long PreAuthRequestId { get; set; }
    public PreAuthRequest? PreAuthRequest { get; set; }

    public decimal RequestedAmount { get; set; }
    public required string Reason { get; set; }

    public long RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }
    public DateTimeOffset RequestedAt { get; set; }

    public PreAuthEnhancementStatus Status { get; set; } = PreAuthEnhancementStatus.Requested;
    public long? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? RejectionReason { get; set; }
}
