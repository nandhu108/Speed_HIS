namespace Prixo.Domain.Enums;

public enum BillStatus
{
    Open = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Cancelled = 4,
    /// <summary>On hold — blocks edits/payments/discounts the same as Cancelled/Paid. Unparking restores the prior status.</summary>
    Parked = 5,
}
