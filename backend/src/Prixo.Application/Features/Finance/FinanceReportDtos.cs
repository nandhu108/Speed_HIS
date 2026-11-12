namespace Prixo.Application.Features.Finance;

public class FinanceReportDateRange
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public record CashBankCollectionRowDto(long PaymentModeId, string PaymentModeName, decimal Amount, int PaymentCount);
public record CashBankCollectionDto(IReadOnlyList<CashBankCollectionRowDto> ByMode, decimal Total);

public record RevenueByDepartmentRowDto(long? DepartmentId, string DepartmentName, decimal Amount);
public record RevenueByDoctorRowDto(long? DoctorId, string DoctorName, decimal Amount);
public record RevenueReportDto(IReadOnlyList<RevenueByDepartmentRowDto> ByDepartment, IReadOnlyList<RevenueByDoctorRowDto> ByDoctor);

public record GstOutwardRowDto(string Source, decimal TaxPercent, decimal TaxableAmount, decimal TaxAmount);
public record HsnSummaryRowDto(string HsnCode, decimal Quantity, decimal TaxableAmount, decimal TaxAmount);
public record GstReportDto(IReadOnlyList<GstOutwardRowDto> OutwardRegister, IReadOnlyList<HsnSummaryRowDto> HsnSummary);

public record OutstandingBillRowDto(long BillId, string BillNumber, DateOnly BillDate, string PatientName, bool IsCreditBill, string? CorporateName, decimal TotalAmount, decimal BalanceAmount);
public record OutstandingReportDto(IReadOnlyList<OutstandingBillRowDto> Bills, decimal TotalOutstanding);

public record DayCloseConsoleRowDto(long BranchId, string BranchName, bool IsClosed, decimal? SystemTotal, decimal? DeclaredTotal, decimal? Variance);

/// <summary>One account's cumulative Debit/Credit totals across every LedgerEntry posted on or before AsOfDate — raw sums, not netted to one side, so TotalDebit/TotalCredit on TrialBalanceDto matching each other is the actual "trial balance" proof that the ledger is in balance.</summary>
public record TrialBalanceRowDto(long AccountId, string AccountCode, string AccountName, string AccountType, decimal DebitTotal, decimal CreditTotal);
public record TrialBalanceDto(DateOnly AsOfDate, IReadOnlyList<TrialBalanceRowDto> Rows, decimal TotalDebit, decimal TotalCredit);
