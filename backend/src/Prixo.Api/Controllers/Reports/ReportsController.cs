using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Reports;

namespace Prixo.Api.Controllers.Reports;

/// <summary>Custom Report Builder v1 (plan §17/§23) — catalog + generic executor + CSV/PDF/Excel export.</summary>
[Route("api/v1/reports")]
public class ReportsController(
    ReportCatalogService catalog, ReportExecutionService execution, IPdfService pdf, IReportExportService excel) : ApiControllerBase
{
    [HttpGet("catalog")]
    [HasPermission(PermissionRegistry.ReportCatalogView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReportCatalogEntryDto>>>> GetCatalog(CancellationToken ct) =>
        OkResponse(await catalog.GetCatalogAsync(ct));

    [HttpPost("{code}/execute")]
    [HasPermission(PermissionRegistry.ReportCatalogView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> Execute(string code, ReportExecuteRequest request, CancellationToken ct) =>
        OkResponse(await execution.ExecuteAsync(code, request, ct));

    [HttpGet("{code}/export")]
    [HasPermission(PermissionRegistry.ReportCatalogView)]
    public async Task<IActionResult> Export(
        string code, [FromQuery] string format, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] bool crossBranch, CancellationToken ct)
    {
        var result = await execution.ExecuteAsync(code, new ReportExecuteRequest(fromDate, toDate, crossBranch), ct);
        return ReportExportHelper.ToFileResult(result, format, code.ToLowerInvariant(), ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }

    [HttpGet("subscriptions")]
    [HasPermission(PermissionRegistry.ReportSubscriptionManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReportSubscriptionDto>>>> GetSubscriptions(
        [FromServices] ReportSubscriptionService subscriptions, CancellationToken ct) =>
        OkResponse(await subscriptions.GetMineAsync(ct));

    [HttpPost("subscriptions")]
    [HasPermission(PermissionRegistry.ReportSubscriptionManage)]
    public async Task<ActionResult<ApiResponse<long>>> CreateSubscription(
        [FromServices] ReportSubscriptionService subscriptions, CreateReportSubscriptionRequest request, CancellationToken ct)
    {
        var id = await subscriptions.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/reports/subscriptions/{id}", id, "Subscription created (delivery is a placeholder until a mailer is wired up).");
    }

    [HttpDelete("subscriptions/{id:long}")]
    [HasPermission(PermissionRegistry.ReportSubscriptionManage)]
    public async Task<ActionResult<ApiResponse>> DeleteSubscription(
        [FromServices] ReportSubscriptionService subscriptions, long id, CancellationToken ct)
    {
        await subscriptions.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Subscription removed.");
    }
}
