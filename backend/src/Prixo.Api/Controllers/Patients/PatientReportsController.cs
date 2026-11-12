using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Api.Controllers.Reports;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Patients;
using Prixo.Application.Features.Reports;

namespace Prixo.Api.Controllers.Patients;

[Route("api/v1/patients/reports")]
public class PatientReportsController(PatientReportService reports, IPdfService pdf, IReportExportService excel) : ApiControllerBase
{
    [HttpGet("block-unblock")]
    [HasPermission(PermissionRegistry.PatientBlock)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> Get(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct) =>
        OkResponse(await reports.GetBlockUnblockReportAsync(new ReportExecuteRequest(fromDate, toDate), ct));

    [HttpGet("block-unblock/export")]
    [HasPermission(PermissionRegistry.PatientBlock)]
    public async Task<IActionResult> Export(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetBlockUnblockReportAsync(new ReportExecuteRequest(fromDate, toDate), ct);
        return ReportExportHelper.ToFileResult(result, format, "patient-block-unblock-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }
}
