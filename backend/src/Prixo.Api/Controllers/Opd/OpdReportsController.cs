using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Api.Controllers.Reports;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Opd;
using Prixo.Application.Features.Reports;

namespace Prixo.Api.Controllers.Opd;

/// <summary>OP Report — standalone date-range/summary-detail report with CSV/Excel/PDF export, distinct from the day/doctor-only OP_CENSUS row in the Custom Report Builder catalog.</summary>
[Route("api/v1/opd/reports")]
public class OpdReportsController(OpReportService reports, IPdfService pdf, IReportExportService excel) : ApiControllerBase
{
    [HttpGet("visits")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetVisits(
        [FromQuery] string? mode, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] long? campId, [FromQuery] long? doctorId, [FromQuery] Domain.Enums.VisitType? visitType,
        [FromQuery] long? patientCategoryId, [FromQuery] long? departmentId, [FromQuery] long? referrerId,
        [FromQuery] long? referralDoctorId, [FromQuery] string? regNo, [FromQuery] string? patientName,
        [FromQuery] bool? isMlcCase, CancellationToken ct) =>
        OkResponse(await reports.GetVisitsReportAsync(
            new OpReportQuery(mode, fromDate, toDate, campId, doctorId, visitType, patientCategoryId,
                departmentId, referrerId, referralDoctorId, regNo, patientName, isMlcCase), ct));

    [HttpGet("visits/export")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<IActionResult> ExportVisits(
        [FromQuery] string? mode, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] long? campId, [FromQuery] long? doctorId, [FromQuery] Domain.Enums.VisitType? visitType,
        [FromQuery] long? patientCategoryId, [FromQuery] long? departmentId, [FromQuery] long? referrerId,
        [FromQuery] long? referralDoctorId, [FromQuery] string? regNo, [FromQuery] string? patientName,
        [FromQuery] bool? isMlcCase, [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetVisitsReportAsync(
            new OpReportQuery(mode, fromDate, toDate, campId, doctorId, visitType, patientCategoryId,
                departmentId, referrerId, referralDoctorId, regNo, patientName, isMlcCase), ct);
        return ReportExportHelper.ToFileResult(result, format, "op-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }

    [HttpGet("cancelled")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetCancelled(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct) =>
        OkResponse(await reports.GetCancelReportAsync(new ReportExecuteRequest(fromDate, toDate), ct));

    [HttpGet("cancelled/export")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<IActionResult> ExportCancelled(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetCancelReportAsync(new ReportExecuteRequest(fromDate, toDate), ct);
        return ReportExportHelper.ToFileResult(result, format, "op-cancel-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }

    [HttpGet("tat")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetTat(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] long? doctorId, CancellationToken ct) =>
        OkResponse(await reports.GetTatReportAsync(new OpTatReportQuery(fromDate, toDate, doctorId), ct));

    [HttpGet("tat/export")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<IActionResult> ExportTat(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] long? doctorId,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetTatReportAsync(new OpTatReportQuery(fromDate, toDate, doctorId), ct);
        return ReportExportHelper.ToFileResult(result, format, "op-tat-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }

    [HttpGet("referrals")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetReferrals(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] Domain.Enums.ReferralDirection? direction, CancellationToken ct) =>
        OkResponse(await reports.GetReferralReportAsync(new ReferralReportQuery(fromDate, toDate, direction), ct));

    [HttpGet("referrals/export")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<IActionResult> ExportReferrals(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] Domain.Enums.ReferralDirection? direction, [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetReferralReportAsync(new ReferralReportQuery(fromDate, toDate, direction), ct);
        return ReportExportHelper.ToFileResult(result, format, "referral-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }

    [HttpGet("conversion")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetConversion(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] string? mode,
        [FromQuery] long? doctorId, [FromQuery] long? departmentId, [FromQuery] long? patientCategoryId,
        [FromQuery] string? regNo, [FromQuery] string? patientName, CancellationToken ct) =>
        OkResponse(await reports.GetConversionReportAsync(
            new ConversionReportQuery(fromDate, toDate, mode, doctorId, departmentId, patientCategoryId, regNo, patientName), ct));

    [HttpGet("conversion/export")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<IActionResult> ExportConversion(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] string? mode,
        [FromQuery] long? doctorId, [FromQuery] long? departmentId, [FromQuery] long? patientCategoryId,
        [FromQuery] string? regNo, [FromQuery] string? patientName,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetConversionReportAsync(
            new ConversionReportQuery(fromDate, toDate, mode, doctorId, departmentId, patientCategoryId, regNo, patientName), ct);
        return ReportExportHelper.ToFileResult(result, format, "op-conversion-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }
}
