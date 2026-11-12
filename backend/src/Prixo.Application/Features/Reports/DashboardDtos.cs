namespace Prixo.Application.Features.Reports;

public record ReportDateRange(DateOnly? FromDate, DateOnly? ToDate);

public record TrendPointDto(DateOnly Date, decimal Amount);
public record DepartmentAmountRowDto(long? DepartmentId, string DepartmentName, decimal Amount);
public record DoctorAmountRowDto(long? DoctorId, string DoctorName, decimal Amount);
public record PaymentModeAmountRowDto(long PaymentModeId, string PaymentModeName, decimal Amount, int TxnCount);
public record DoctorVisitCountRowDto(long DoctorId, string DoctorName, int VisitCount);

public record ManagementDashboardDto(
    decimal TotalRevenue, decimal TotalCollection, int OpVisitCount, int ActiveInpatients,
    decimal BedOccupancyPercent, int UnacknowledgedAlerts, bool IsCrossBranch,
    IReadOnlyList<BranchSummaryRowDto> ByBranch);

public record BranchSummaryRowDto(long BranchId, string BranchName, decimal Revenue, decimal Collection, int OpVisitCount, int ActiveInpatients);

public record OpDashboardDto(
    int TotalVisits, int NewCount, int ReviewCount, IReadOnlyList<TrendPointDto> DailyTrend, IReadOnlyList<DoctorVisitCountRowDto> ByDoctor);

public record IpDashboardDto(
    int TotalBeds, int Occupied, int Vacant, decimal OccupancyRatePercent, int AdmissionsInRange, int DischargesInRange, decimal? AverageLengthOfStayDays);

public record BillingDashboardDto(
    decimal TotalCollection, IReadOnlyList<PaymentModeAmountRowDto> ByMode, IReadOnlyList<TrendPointDto> DailyTrend, decimal OutstandingAmount);

public record RevenueDashboardDto(
    decimal TotalRevenue, IReadOnlyList<TrendPointDto> DailyTrend,
    IReadOnlyList<DepartmentAmountRowDto> ByDepartment, IReadOnlyList<DoctorAmountRowDto> ByDoctor);
