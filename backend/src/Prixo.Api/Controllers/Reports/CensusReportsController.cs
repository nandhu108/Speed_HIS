using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Reports;
using Prixo.Domain.Enums;

namespace Prixo.Api.Controllers.Reports;

/// <summary>OPIP Census Report — department-wise New/Review×Gender/Child counts blending OP visits
/// and IP admissions (query.Scope picks Both/Op/Ip), plus the patient-wise detail listing behind it.</summary>
[Route("api/v1/reports/census")]
public class CensusReportsController(CensusReportService reports, IPdfService pdf, IReportExportService excel) : ApiControllerBase
{
    [HttpGet("department-wise")]
    [HasPermission(PermissionRegistry.ReportCensusView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetDepartmentWise(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] CensusScope scope,
        [FromQuery] long? departmentId, [FromQuery] long? wardId, [FromQuery] long? patientCategoryId,
        CancellationToken ct) =>
        OkResponse(await reports.GetDepartmentWiseCensusAsync(
            new CensusReportQuery(fromDate, toDate, scope, departmentId, wardId, patientCategoryId), ct));

    [HttpGet("department-wise/export")]
    [HasPermission(PermissionRegistry.ReportCensusView)]
    public async Task<IActionResult> ExportDepartmentWise(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] CensusScope scope,
        [FromQuery] long? departmentId, [FromQuery] long? wardId, [FromQuery] long? patientCategoryId,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetDepartmentWiseCensusAsync(
            new CensusReportQuery(fromDate, toDate, scope, departmentId, wardId, patientCategoryId), ct);
        return ReportExportHelper.ToFileResult(result, format, "opip-census-department-wise", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }

    [HttpGet("patient-wise")]
    [HasPermission(PermissionRegistry.ReportCensusView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetPatientWise(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] CensusScope scope,
        [FromQuery] long? departmentId, [FromQuery] long? wardId, [FromQuery] long? patientCategoryId,
        CancellationToken ct) =>
        OkResponse(await reports.GetPatientWiseDetailsAsync(
            new CensusReportQuery(fromDate, toDate, scope, departmentId, wardId, patientCategoryId), ct));

    [HttpGet("patient-wise/export")]
    [HasPermission(PermissionRegistry.ReportCensusView)]
    public async Task<IActionResult> ExportPatientWise(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] CensusScope scope,
        [FromQuery] long? departmentId, [FromQuery] long? wardId, [FromQuery] long? patientCategoryId,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetPatientWiseDetailsAsync(
            new CensusReportQuery(fromDate, toDate, scope, departmentId, wardId, patientCategoryId), ct);
        return ReportExportHelper.ToFileResult(result, format, "opip-census-patient-wise", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }
}
