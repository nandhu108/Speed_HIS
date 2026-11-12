namespace Prixo.Domain.Enums;

/// <summary>
/// Shared Draft/Posted/Cancelled lifecycle for the simpler stock documents (GRN, purchase
/// return, sales return, issue return) — posting is what writes the StockLedger rows, so a
/// document can be reviewed before it touches stock.
/// </summary>
public enum DocumentStatus
{
    Draft = 1,
    Posted = 2,
    Cancelled = 3,
}
