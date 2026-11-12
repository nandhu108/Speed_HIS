namespace Prixo.Domain.Enums;

public enum CashierShiftStatus
{
    Open = 1,
    /// <summary>Cashier declared amounts and closed the shift; cash still in the cashier's custody.</summary>
    Closed = 2,
    /// <summary>A supervisor confirmed physical cash was handed over.</summary>
    Received = 3,
}
