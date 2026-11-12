using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Api.Controllers.Reports;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Er;
using Prixo.Application.Features.Reports;

namespace Prixo.Api.Controllers.Er;

/// <summary>ER Report — Summary/Detail breakdown of Emergency/Casualty visits, date-range and
/// filterable, with CSV/Excel/PDF export via the shared ReportExportHelper.</summary>
[Route("api/v1/emergency/reports")]
public class ErReportsController(ErReportService reports, IPdfService pdf, IReportExportService excel) : ApiControllerBase
{
    [HttpGet("visits")]
    [HasPermission(PermissionRegistry.EmergencyVisitView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetVisits(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] string? mode,
        [FromQuery] long? doctorId, [FromQuery] Domain.Enums.EsiLevel? esiLevel, [FromQuery] Domain.Enums.EmergencyStatus? status,
        [FromQuery] Domain.Enums.DispositionType? disposition, [FromQuery] bool? isMlc, [FromQuery] long? patientCategoryId,
        [FromQuery] string? regNo, [FromQuery] string? patientName, CancellationToken ct) =>
        OkResponse(await reports.GetErReportAsync(
            new ErReportQuery(fromDate, toDate, mode, doctorId, esiLevel, status, disposition, isMlc, patientCategoryId, regNo, patientName), ct));

    [HttpGet("visits/export")]
    [HasPermission(PermissionRegistry.EmergencyVisitView)]
    public async Task<IActionResult> ExportVisits(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] string? mode,
        [FromQuery] long? doctorId, [FromQuery] Domain.Enums.EsiLevel? esiLevel, [FromQuery] Domain.Enums.EmergencyStatus? status,
        [FromQuery] Domain.Enums.DispositionType? disposition, [FromQuery] bool? isMlc, [FromQuery] long? patientCategoryId,
        [FromQuery] string? regNo, [FromQuery] string? patientName,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetErReportAsync(
            new ErReportQuery(fromDate, toDate, mode, doctorId, esiLevel, status, disposition, isMlc, patientCategoryId, regNo, patientName), ct);
        return ReportExportHelper.ToFileResult(result, format, "er-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }
}
