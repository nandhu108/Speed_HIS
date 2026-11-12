using Prixo.Domain.Common;

namespace Prixo.Domain.Entities.Billing;

public class BillPayment : BaseEntity
{
    public long BillId { get; set; }
    public Bill? Bill { get; set; }
    public long PaymentModeId { get; set; }
    public PaymentMode? PaymentMode { get; set; }

    public decimal Amount { get; set; }
    /// <summary>Card/UPI/Cheque transaction reference; null for cash.</summary>
    public string? ReferenceNumber { get; set; }
    public required string ReceiptNumber { get; set; }
    public DateTimeOffset PaidAt { get; set; }

    /// <summary>Set from the collecting user's currently-open CashierShift, if any — null when no shift is open (day-level DayClose reconciliation keeps working either way).</summary>
    public long? CashierShiftId { get; set; }
    public CashierShift? CashierShift { get; set; }
}
