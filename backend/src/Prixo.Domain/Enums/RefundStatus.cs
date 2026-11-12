namespace Prixo.Domain.Enums;

public enum RefundStatus
{
    Pending = 1,
    /// <summary>A decision to pay out has been made, but the money hasn't moved yet — see Issued.</summary>
    Approved = 2,
    Rejected = 3,
    /// <summary>The approved refund has actually been paid out (bill balance/advance balance adjusted) — a separate step from Approved, same two-step shape as CashierShiftStatus.Closed → Received.</summary>
    Issued = 4,
}
