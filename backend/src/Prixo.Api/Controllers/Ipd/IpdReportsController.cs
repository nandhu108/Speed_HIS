using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Api.Controllers.Reports;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Ipd;
using Prixo.Application.Features.Reports;

namespace Prixo.Api.Controllers.Ipd;

/// <summary>IP Report (admissions) and Ward Occupancy Report — standalone date-range/summary-detail reports with CSV/Excel/PDF export.</summary>
[Route("api/v1/ipd/reports")]
public class IpdReportsController(IpReportService reports, IPdfService pdf, IReportExportService excel) : ApiControllerBase
{
    [HttpGet("admissions")]
    [HasPermission(PermissionRegistry.AdmissionView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetAdmissions(
        [FromQuery] string? mode, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] long? doctorId, [FromQuery] long? wardId, [FromQuery] long? departmentId,
        [FromQuery] long? patientCategoryId, [FromQuery] long? referrerId, [FromQuery] long? roomTypeId,
        [FromQuery] Domain.Enums.AdmissionStatus? status, [FromQuery] long? unitId,
        [FromQuery] string? regNo, [FromQuery] string? patientName, [FromQuery] string? ipNo,
        CancellationToken ct) =>
        OkResponse(await reports.GetAdmissionsReportAsync(
            new IpReportQuery(mode, fromDate, toDate, doctorId, wardId, departmentId, patientCategoryId,
                referrerId, roomTypeId, status, unitId, regNo, patientName, ipNo), ct));

    [HttpGet("admissions/export")]
    [HasPermission(PermissionRegistry.AdmissionView)]
    public async Task<IActionResult> ExportAdmissions(
        [FromQuery] string? mode, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] long? doctorId, [FromQuery] long? wardId, [FromQuery] long? departmentId,
        [FromQuery] long? patientCategoryId, [FromQuery] long? referrerId, [FromQuery] long? roomTypeId,
        [FromQuery] Domain.Enums.AdmissionStatus? status, [FromQuery] long? unitId,
        [FromQuery] string? regNo, [FromQuery] string? patientName, [FromQuery] string? ipNo,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetAdmissionsReportAsync(
            new IpReportQuery(mode, fromDate, toDate, doctorId, wardId, departmentId, patientCategoryId,
                referrerId, roomTypeId, status, unitId, regNo, patientName, ipNo), ct);
        return ReportExportHelper.ToFileResult(result, format, "ip-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }

    [HttpGet("ward-occupancy")]
    [HasPermission(PermissionRegistry.BedView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetWardOccupancy(
        [FromQuery] string? mode, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] long? wardId, [FromQuery] long? doctorId, [FromQuery] long? unitId,
        [FromQuery] string? wardFlag, [FromQuery] Domain.Enums.BedStatus? bedStatus, CancellationToken ct) =>
        OkResponse(await reports.GetWardOccupancyReportAsync(
            new WardOccupancyReportQuery(mode, fromDate, toDate, wardId, doctorId, unitId, wardFlag, bedStatus), ct));

    [HttpGet("ward-occupancy/export")]
    [HasPermission(PermissionRegistry.BedView)]
    public async Task<IActionResult> ExportWardOccupancy(
        [FromQuery] string? mode, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] long? wardId, [FromQuery] long? doctorId, [FromQuery] long? unitId,
        [FromQuery] string? wardFlag, [FromQuery] Domain.Enums.BedStatus? bedStatus,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetWardOccupancyReportAsync(
            new WardOccupancyReportQuery(mode, fromDate, toDate, wardId, doctorId, unitId, wardFlag, bedStatus), ct);
        return ReportExportHelper.ToFileResult(result, format, "ward-occupancy-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }

    [HttpGet("admission-request-conversion")]
    [HasPermission(PermissionRegistry.AdmissionRequestView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> GetAdmissionRequestConversion(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] long? departmentId, [FromQuery] long? doctorId, [FromQuery] long? advisedDoctorId,
        [FromQuery] long? wardId, [FromQuery] long? roomTypeId, [FromQuery] long? patientCategoryId,
        [FromQuery] Domain.Enums.AdmissionRequestStatus? status, [FromQuery] string? regNo, [FromQuery] string? patientName,
        CancellationToken ct) =>
        OkResponse(await reports.GetAdmissionRequestConversionReportAsync(
            new AdmissionRequestConversionReportQuery(fromDate, toDate, departmentId, doctorId, advisedDoctorId,
                wardId, roomTypeId, patientCategoryId, status, regNo, patientName), ct));

    [HttpGet("admission-request-conversion/export")]
    [HasPermission(PermissionRegistry.AdmissionRequestView)]
    public async Task<IActionResult> ExportAdmissionRequestConversion(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] long? departmentId, [FromQuery] long? doctorId, [FromQuery] long? advisedDoctorId,
        [FromQuery] long? wardId, [FromQuery] long? roomTypeId, [FromQuery] long? patientCategoryId,
        [FromQuery] Domain.Enums.AdmissionRequestStatus? status, [FromQuery] string? regNo, [FromQuery] string? patientName,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetAdmissionRequestConversionReportAsync(
            new AdmissionRequestConversionReportQuery(fromDate, toDate, departmentId, doctorId, advisedDoctorId,
                wardId, roomTypeId, patientCategoryId, status, regNo, patientName), ct);
        return ReportExportHelper.ToFileResult(result, format, "doctor-admission-request-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }
}
