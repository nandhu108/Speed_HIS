using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Billing;

/// <summary>
/// Refund request against a bill (optionally a specific payment) or against an advance's
/// unused deposit — exactly one of BillId/AdvanceId is set, enforced by a DB check constraint
/// (ck_refunds_bill_or_advance) as well as at the service layer. Gated behind a
/// two-step Approve-then-Issue workflow: Approved is a decision, Issued is money actually
/// moving (bill balance / advance balance adjusted) — same shape as CashierShiftStatus's
/// Closed → Received.
/// </summary>
public class Refund : BaseEntity
{
    public long? BillId { get; set; }
    public Bill? Bill { get; set; }
    public long? BillPaymentId { get; set; }
    public BillPayment? BillPayment { get; set; }
    public long? AdvanceId { get; set; }
    public Advance? Advance { get; set; }

    public decimal Amount { get; set; }
    public required string Reason { get; set; }
    public RefundMethod Method { get; set; } = RefundMethod.Cash;
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public long? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public long? IssuedByUserId { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public string? RejectionReason { get; set; }
}
