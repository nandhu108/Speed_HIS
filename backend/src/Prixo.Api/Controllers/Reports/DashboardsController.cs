using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Reports;

namespace Prixo.Api.Controllers.Reports;

/// <summary>Cross-cutting Phase 23 dashboards — see DashboardService's doc comment.</summary>
[Route("api/v1/dashboards")]
public class DashboardsController(DashboardService dashboards) : ApiControllerBase
{
    [HttpGet("management")]
    [HasPermission(PermissionRegistry.DashboardManagementView)]
    public async Task<ActionResult<ApiResponse<ManagementDashboardDto>>> GetManagement(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct) =>
        OkResponse(await dashboards.GetManagementDashboardAsync(new ReportDateRange(fromDate, toDate), ct));

    [HttpGet("op")]
    [HasPermission(PermissionRegistry.DashboardOpView)]
    public async Task<ActionResult<ApiResponse<OpDashboardDto>>> GetOp(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct) =>
        OkResponse(await dashboards.GetOpDashboardAsync(new ReportDateRange(fromDate, toDate), ct));

    [HttpGet("ip")]
    [HasPermission(PermissionRegistry.DashboardIpView)]
    public async Task<ActionResult<ApiResponse<IpDashboardDto>>> GetIp(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct) =>
        OkResponse(await dashboards.GetIpDashboardAsync(new ReportDateRange(fromDate, toDate), ct));

    [HttpGet("billing")]
    [HasPermission(PermissionRegistry.DashboardBillingView)]
    public async Task<ActionResult<ApiResponse<BillingDashboardDto>>> GetBilling(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct) =>
        OkResponse(await dashboards.GetBillingDashboardAsync(new ReportDateRange(fromDate, toDate), ct));

    [HttpGet("revenue")]
    [HasPermission(PermissionRegistry.DashboardRevenueView)]
    public async Task<ActionResult<ApiResponse<RevenueDashboardDto>>> GetRevenue(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct) =>
        OkResponse(await dashboards.GetRevenueDashboardAsync(new ReportDateRange(fromDate, toDate), ct));
}
