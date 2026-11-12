using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Api.Controllers.Reports;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Opd;
using Prixo.Application.Features.Reports;

namespace Prixo.Api.Controllers.Opd;

/// <summary>MHC's four reports (Package-wise/Doctor-wise/Referrer-wise/Patient List) reuse
/// OpReportService's engine — same `opd.op_visits` table, Category == Mhc, gated on MHC's own
/// permission rather than OPD's. Service List Report is a distinct report over MHC visits' billed
/// Service line items, not the same query shape.</summary>
[Route("api/v1/mhc/reports")]
public class MhcReportsController(OpReportService reports, IPdfService pdf, IReportExportService excel) : ApiControllerBase
{
    [HttpGet("package-wise")]
    [HasPermission(PermissionRegistry.MhcVisitView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetPackageWise(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] long? mhcDoctorId,
        [FromQuery] Domain.Enums.VisitType? visitType, [FromQuery] long? patientCategoryId,
        [FromQuery] string? regNo, [FromQuery] string? patientName, CancellationToken ct) =>
        OkResponse(await reports.GetMhcPackageWiseReportAsync(
            new MhcReportQuery(fromDate, toDate, mhcDoctorId, visitType, patientCategoryId, regNo, patientName), ct));

    [HttpGet("package-wise/export")]
    [HasPermission(PermissionRegistry.MhcVisitView)]
    public async Task<IActionResult> ExportPackageWise(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] long? mhcDoctorId,
        [FromQuery] Domain.Enums.VisitType? visitType, [FromQuery] long? patientCategoryId,
        [FromQuery] string? regNo, [FromQuery] string? patientName,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetMhcPackageWiseReportAsync(
            new MhcReportQuery(fromDate, toDate, mhcDoctorId, visitType, patientCategoryId, regNo, patientName), ct);
        return ReportExportHelper.ToFileResult(result, format, "mhc-package-wise-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }

    [HttpGet("doctor-wise")]
    [HasPermission(PermissionRegistry.MhcVisitView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetDoctorWise(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] long? mhcDoctorId,
        [FromQuery] Domain.Enums.VisitType? visitType, [FromQuery] long? patientCategoryId,
        [FromQuery] string? regNo, [FromQuery] string? patientName, CancellationToken ct) =>
        OkResponse(await reports.GetMhcDoctorWiseReportAsync(
            new MhcReportQuery(fromDate, toDate, mhcDoctorId, visitType, patientCategoryId, regNo, patientName), ct));

    [HttpGet("doctor-wise/export")]
    [HasPermission(PermissionRegistry.MhcVisitView)]
    public async Task<IActionResult> ExportDoctorWise(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] long? mhcDoctorId,
        [FromQuery] Domain.Enums.VisitType? visitType, [FromQuery] long? patientCategoryId,
        [FromQuery] string? regNo, [FromQuery] string? patientName,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetMhcDoctorWiseReportAsync(
            new MhcReportQuery(fromDate, toDate, mhcDoctorId, visitType, patientCategoryId, regNo, patientName), ct);
        return ReportExportHelper.ToFileResult(result, format, "mhc-doctor-wise-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }

    [HttpGet("referrer-wise")]
    [HasPermission(PermissionRegistry.MhcVisitView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetReferrerWise(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] long? mhcDoctorId,
        [FromQuery] Domain.Enums.VisitType? visitType, [FromQuery] long? patientCategoryId,
        [FromQuery] string? regNo, [FromQuery] string? patientName, CancellationToken ct) =>
        OkResponse(await reports.GetMhcReferrerWiseReportAsync(
            new MhcReportQuery(fromDate, toDate, mhcDoctorId, visitType, patientCategoryId, regNo, patientName), ct));

    [HttpGet("referrer-wise/export")]
    [HasPermission(PermissionRegistry.MhcVisitView)]
    public async Task<IActionResult> ExportReferrerWise(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] long? mhcDoctorId,
        [FromQuery] Domain.Enums.VisitType? visitType, [FromQuery] long? patientCategoryId,
        [FromQuery] string? regNo, [FromQuery] string? patientName,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetMhcReferrerWiseReportAsync(
            new MhcReportQuery(fromDate, toDate, mhcDoctorId, visitType, patientCategoryId, regNo, patientName), ct);
        return ReportExportHelper.ToFileResult(result, format, "mhc-referrer-wise-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }

    [HttpGet("patient-list")]
    [HasPermission(PermissionRegistry.MhcVisitView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetPatientList(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] long? mhcDoctorId,
        [FromQuery] Domain.Enums.VisitType? visitType, [FromQuery] long? patientCategoryId,
        [FromQuery] string? regNo, [FromQuery] string? patientName, CancellationToken ct) =>
        OkResponse(await reports.GetMhcPatientListReportAsync(
            new MhcReportQuery(fromDate, toDate, mhcDoctorId, visitType, patientCategoryId, regNo, patientName), ct));

    [HttpGet("patient-list/export")]
    [HasPermission(PermissionRegistry.MhcVisitView)]
    public async Task<IActionResult> ExportPatientList(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] long? mhcDoctorId,
        [FromQuery] Domain.Enums.VisitType? visitType, [FromQuery] long? patientCategoryId,
        [FromQuery] string? regNo, [FromQuery] string? patientName,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetMhcPatientListReportAsync(
            new MhcReportQuery(fromDate, toDate, mhcDoctorId, visitType, patientCategoryId, regNo, patientName), ct);
        return ReportExportHelper.ToFileResult(result, format, "mhc-patient-list-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }

    [HttpGet("service-list")]
    [HasPermission(PermissionRegistry.MhcVisitView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetServiceList(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] long? serviceId,
        [FromQuery] Domain.Enums.BillType? billType, [FromQuery] string? regNo, CancellationToken ct) =>
        OkResponse(await reports.GetMhcServiceListReportAsync(new MhcServiceListReportQuery(fromDate, toDate, serviceId, billType, regNo), ct));

    [HttpGet("service-list/export")]
    [HasPermission(PermissionRegistry.MhcVisitView)]
    public async Task<IActionResult> ExportServiceList(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] long? serviceId,
        [FromQuery] Domain.Enums.BillType? billType, [FromQuery] string? regNo,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetMhcServiceListReportAsync(new MhcServiceListReportQuery(fromDate, toDate, serviceId, billType, regNo), ct);
        return ReportExportHelper.ToFileResult(result, format, "mhc-service-list-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }
}
