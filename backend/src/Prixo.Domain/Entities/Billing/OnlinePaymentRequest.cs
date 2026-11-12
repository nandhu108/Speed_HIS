using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Billing;

/// <summary>
/// Payment outbox placeholder (plan §21: "Payment (placeholder — gateway held)") — same posture
/// as Appointments' <see cref="Prixo.Domain.Entities.Admin.NotificationQueue"/>: real schema
/// captured now, rows are never processed until a payment gateway (held module) adds a
/// processor. A patient "requesting" online payment today just queues intent for the billing
/// desk to follow up on; no money moves through this row. Not a BaseEntity — infra/outbox
/// table, not a business/audit record (same reasoning as NotificationQueue).
/// </summary>
public class OnlinePaymentRequest
{
    public long Id { get; set; }
    public long BillId { get; set; }
    public Bill? Bill { get; set; }
    public long PatientId { get; set; }
    public decimal Amount { get; set; }
    public OnlinePaymentRequestStatus Status { get; set; } = OnlinePaymentRequestStatus.Requested;
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
