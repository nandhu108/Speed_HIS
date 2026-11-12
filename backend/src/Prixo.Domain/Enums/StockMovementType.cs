namespace Prixo.Domain.Enums;

/// <summary>
/// Every StockLedger row carries exactly one of these — the sign (in/out) is implied by
/// the type, not stored separately, so a row can never claim to be both.
/// </summary>
public enum StockMovementType
{
    PurchaseIn = 1,
    PurchaseReturnOut = 2,
    SaleOut = 3,
    SaleReturnIn = 4,
    IssueOut = 5,
    IssueReturnIn = 6,
    /// <summary>Also used to post Stock Audit variances — an audit variance is just an adjustment with a different origin document.</summary>
    AdjustmentIn = 7,
    AdjustmentOut = 8,
    TransferOut = 9,
    TransferIn = 10,
}
