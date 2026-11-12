using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Finance;

namespace Prixo.Api.Controllers.Finance;

[Route("api/v1/finance/reports")]
public class FinanceReportsController(FinanceReportService reports) : ApiControllerBase
{
    [HttpGet("cash-bank-collection")]
    [HasPermission(PermissionRegistry.FinanceReportView)]
    public async Task<ActionResult<ApiResponse<CashBankCollectionDto>>> GetCashBankCollection([FromQuery] FinanceReportDateRange range, CancellationToken ct) =>
        OkResponse(await reports.GetCashBankCollectionAsync(range, ct));

    [HttpGet("revenue")]
    [HasPermission(PermissionRegistry.FinanceReportView)]
    public async Task<ActionResult<ApiResponse<RevenueReportDto>>> GetRevenue([FromQuery] FinanceReportDateRange range, CancellationToken ct) =>
        OkResponse(await reports.GetRevenueReportAsync(range, ct));

    [HttpGet("gst")]
    [HasPermission(PermissionRegistry.FinanceReportView)]
    public async Task<ActionResult<ApiResponse<GstReportDto>>> GetGst([FromQuery] FinanceReportDateRange range, CancellationToken ct) =>
        OkResponse(await reports.GetGstReportAsync(range, ct));

    [HttpGet("outstanding")]
    [HasPermission(PermissionRegistry.FinanceReportView)]
    public async Task<ActionResult<ApiResponse<OutstandingReportDto>>> GetOutstanding(CancellationToken ct) =>
        OkResponse(await reports.GetOutstandingReportAsync(ct));

    [HttpGet("trial-balance")]
    [HasPermission(PermissionRegistry.FinanceReportView)]
    public async Task<ActionResult<ApiResponse<TrialBalanceDto>>> GetTrialBalance([FromQuery] DateOnly? asOfDate, CancellationToken ct) =>
        OkResponse(await reports.GetTrialBalanceAsync(asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow), ct));

    /// <summary>The centralized "all counters" day-close console — cross-branch, so it needs both the Finance permission and the general cross-branch-read permission.</summary>
    [HttpGet("day-close-console")]
    [HasPermission(PermissionRegistry.FinanceDayCloseConsoleView)]
    [HasPermission(PermissionRegistry.ReportCrossBranchView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DayCloseConsoleRowDto>>>> GetDayCloseConsole([FromQuery] DateOnly date, CancellationToken ct) =>
        OkResponse(await reports.GetDayCloseConsoleAsync(date, ct));
}
