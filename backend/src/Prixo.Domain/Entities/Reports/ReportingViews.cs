namespace Prixo.Domain.Entities.Reports;

/// <summary>
/// Keyless EF query types mapped onto the `reporting` schema's materialized views (plan §17:
/// "Reporting reads from SQL views / materialized views — never raw OLTP joins scattered in
/// code"). Refreshed periodically by ReportRefreshJob, never written to directly.
/// </summary>
public class RevenueDailyRow
{
    public long BranchId { get; set; }
    public DateOnly RevenueDate { get; set; }
    public long? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public long? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public decimal Amount { get; set; }
}

public class DailyCollectionRow
{
    public long BranchId { get; set; }
    public DateOnly CollectionDate { get; set; }
    public long PaymentModeId { get; set; }
    public string PaymentModeName { get; set; } = "";
    public decimal Amount { get; set; }
    public int TxnCount { get; set; }
}

public class OpCensusRow
{
    public long BranchId { get; set; }
    public DateOnly VisitDate { get; set; }
    public long DoctorId { get; set; }
    public string DoctorName { get; set; } = "";
    public int VisitType { get; set; }
    public int VisitCount { get; set; }
}
