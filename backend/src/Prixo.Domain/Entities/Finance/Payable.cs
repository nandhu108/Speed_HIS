using Prixo.Domain.Common;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Pharmacy;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Finance;

/// <summary>
/// Payables (plan §18: "supplier invoices from GRN → payment") — one row per posted GRN,
/// raised explicitly by Finance (not auto-created the moment a GRN posts, so Finance controls
/// when a supplier invoice enters the AP workflow). Paying one posts a Payment voucher via
/// VoucherService (Debit Accounts Payable / Credit the chosen bank-cash account).
/// </summary>
public class Payable : BranchEntity
{
    public long VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public long GrnId { get; set; }
    public Grn? Grn { get; set; }

    public required string InvoiceNumber { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public decimal InvoiceAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public DateOnly? DueDate { get; set; }
    public PayableStatus Status { get; set; } = PayableStatus.Open;
}

/// <summary>Receivables (plan §18: "credit/corporate outstanding") — one row per credit Bill, the AR mirror of Payable.</summary>
public class Receivable : BranchEntity
{
    public long CorporateId { get; set; }
    public Corporate? Corporate { get; set; }
    public long BillId { get; set; }
    public Bill? Bill { get; set; }

    public DateOnly BillDate { get; set; }
    public decimal BillAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public ReceivableStatus Status { get; set; } = ReceivableStatus.Open;
}
