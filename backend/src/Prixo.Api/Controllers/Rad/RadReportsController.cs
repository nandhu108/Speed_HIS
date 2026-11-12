using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Rad;
using Prixo.Domain.Enums;

namespace Prixo.Api.Controllers.Rad;

[Route("api/v1/rad")]
public class RadReportsController(RadOrderService orders) : ApiControllerBase
{
    [HttpGet("worklist")]
    [HasPermission(PermissionRegistry.RadReportCreate)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReportingWorklistItemDto>>>> GetEntryQueue(CancellationToken ct) =>
        OkResponse(await orders.GetReportingEntryQueueAsync(ct));

    [HttpGet("reports/queue")]
    [HasPermission(PermissionRegistry.RadReportVerify)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReportingWorklistItemDto>>>> GetReviewQueue(
        [FromQuery] RadOrderItemStatus status, CancellationToken ct) =>
        OkResponse(await orders.GetReportingReviewQueueAsync(status, ct));

    [HttpGet("order-items/{itemId:long}/report")]
    [HasPermission(PermissionRegistry.RadOrderView)]
    public async Task<ActionResult<ApiResponse<RadReportDto>>> GetReport(long itemId, CancellationToken ct) =>
        OkResponse(await orders.GetReportByItemIdAsync(itemId, ct));

    [HttpPost("order-items/{itemId:long}/report")]
    [HasPermission(PermissionRegistry.RadReportCreate)]
    public async Task<ActionResult<ApiResponse<RadReportDto>>> EnterReport(
        long itemId, EnterReportRequest request, CancellationToken ct) =>
        OkResponse(await orders.EnterReportAsync(itemId, request, ct), "Report saved.");

    [HttpPost("order-items/{itemId:long}/report/verify")]
    [HasPermission(PermissionRegistry.RadReportVerify)]
    public async Task<ActionResult<ApiResponse<RadReportDto>>> VerifyReport(long itemId, CancellationToken ct) =>
        OkResponse(await orders.VerifyReportAsync(itemId, ct), "Report verified.");

    [HttpPost("order-items/{itemId:long}/report/approve")]
    [HasPermission(PermissionRegistry.RadReportApprove)]
    public async Task<ActionResult<ApiResponse<RadReportDto>>> ApproveReport(long itemId, CancellationToken ct) =>
        OkResponse(await orders.ApproveReportAsync(itemId, ct), "Report approved.");

    [HttpGet("order-items/{itemId:long}/report/pdf")]
    [HasPermission(PermissionRegistry.RadReportView)]
    public async Task<IActionResult> GetReportPdf(long itemId, CancellationToken ct)
    {
        var pdf = await orders.GenerateReportPdfAsync(itemId, ct);
        return File(pdf, "application/pdf", $"radiology-report-{itemId}.pdf");
    }

    [HttpPost("order-items/{itemId:long}/contrast-consent")]
    [HasPermission(PermissionRegistry.RadContrastConsentCapture)]
    public async Task<ActionResult<ApiResponse<ContrastConsentDto>>> CaptureContrastConsent(
        long itemId, CaptureContrastConsentRequest request, CancellationToken ct)
    {
        var consent = await orders.CaptureContrastConsentAsync(itemId, request, ct);
        return CreatedResponse($"/api/v1/rad/order-items/{itemId}/contrast-consent", consent, "Contrast consent recorded.");
    }
}
