using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Api.Controllers.Reports;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Billing;
using Prixo.Application.Features.Reports;
using Prixo.Domain.Enums;

namespace Prixo.Api.Controllers.Billing;

[Route("api/v1/billing/reports")]
[HasPermission(PermissionRegistry.BillingReportView)]
public class BillingReportsController(BillingReportService reports, IReportExportService excel, IPdfService pdf) : ApiControllerBase
{
    /// <summary>
    /// Generic (columns, rows) result — same OpReportPage/OpdReportsController pattern (a JSON
    /// action and an /export action calling the exact same dispatcher, so the on-screen table
    /// and the exported file never drift). One action covers every report key here (ageing,
    /// ward-wise-outstanding, day-book, cancelled-bills, concession, refund, advance,
    /// tariff-list, doctors-payout, user-settlement, service-category-breakup,
    /// ip-service-billing-detail, revenue) instead of one typed action per report — the
    /// frontend's ReportResultTable renders any of them the same way. Every filter param below
    /// only applies to the report key(s) that actually use it; every other key ignores it.
    /// </summary>
    [HttpGet("{key}")]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetResult(
        string key, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] DateOnly? date,
        [FromQuery] BillType? billType, [FromQuery] long? billedByUserId, [FromQuery] long? corporateId,
        [FromQuery] long? doctorId, [FromQuery] long? serviceId, [FromQuery] long? serviceCategoryId, [FromQuery] long? departmentId,
        [FromQuery] bool? isCredit, [FromQuery] string? reportType, [FromQuery] string? ipNumber, [FromQuery] long? wardId,
        [FromQuery] long? patientCategoryId, CancellationToken ct) =>
        OkResponse(await reports.GetReportResultAsync(
            key, new BillingReportDateRange { FromDate = fromDate, ToDate = toDate }, date, billType, billedByUserId, corporateId,
            doctorId, serviceId, serviceCategoryId, departmentId, isCredit, reportType, ipNumber, wardId, patientCategoryId, ct));

    /// <summary>CSV/Excel/PDF export — same dispatcher as GetResult above, just piped through ReportExportHelper instead of returned as JSON.</summary>
    [HttpGet("{key}/export")]
    public async Task<IActionResult> Export(
        string key, [FromQuery] string format, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] DateOnly? date,
        [FromQuery] BillType? billType, [FromQuery] long? billedByUserId, [FromQuery] long? corporateId,
        [FromQuery] long? doctorId, [FromQuery] long? serviceId, [FromQuery] long? serviceCategoryId, [FromQuery] long? departmentId,
        [FromQuery] bool? isCredit, [FromQuery] string? reportType, [FromQuery] string? ipNumber, [FromQuery] long? wardId,
        [FromQuery] long? patientCategoryId, CancellationToken ct)
    {
        var result = await reports.GetReportResultAsync(
            key, new BillingReportDateRange { FromDate = fromDate, ToDate = toDate }, date, billType, billedByUserId, corporateId,
            doctorId, serviceId, serviceCategoryId, departmentId, isCredit, reportType, ipNumber, wardId, patientCategoryId, ct);
        return ReportExportHelper.ToFileResult(result, format, key, ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }

    [HttpGet("concession/search")]
    public async Task<ActionResult<ApiResponse<PagedResult<ConcessionSearchRowDto>>>> SearchConcessions([FromQuery] ConcessionSearchQuery query, CancellationToken ct) =>
        OkResponse(await reports.SearchConcessionsAsync(query, ct));
}
