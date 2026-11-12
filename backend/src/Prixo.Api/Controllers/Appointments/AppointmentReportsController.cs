using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Api.Controllers.Reports;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Appointments;
using Prixo.Application.Features.Reports;

namespace Prixo.Api.Controllers.Appointments;

[Route("api/v1/appointments/reports")]
public class AppointmentReportsController(AppointmentReportService reports, IPdfService pdf, IReportExportService excel) : ApiControllerBase
{
    [HttpGet("doctor-appointment")]
    [HasPermission(PermissionRegistry.AppointmentView)]
    public async Task<ActionResult<ApiResponse<ReportResultDto>>> Get(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] string? mode,
        [FromQuery] long? doctorId, [FromQuery] long? departmentId, [FromQuery] long? unitId,
        [FromQuery] Domain.Enums.AppointmentStatus? status, [FromQuery] string? regNo,
        [FromQuery] string? patientName, [FromQuery] string? mobile, CancellationToken ct) =>
        OkResponse(await reports.GetDoctorAppointmentReportAsync(
            new DoctorAppointmentReportQuery(fromDate, toDate, mode, doctorId, departmentId, unitId, status, regNo, patientName, mobile), ct));

    [HttpGet("doctor-appointment/export")]
    [HasPermission(PermissionRegistry.AppointmentView)]
    public async Task<IActionResult> Export(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] string? mode,
        [FromQuery] long? doctorId, [FromQuery] long? departmentId, [FromQuery] long? unitId,
        [FromQuery] Domain.Enums.AppointmentStatus? status, [FromQuery] string? regNo,
        [FromQuery] string? patientName, [FromQuery] string? mobile,
        [FromQuery] string format, CancellationToken ct)
    {
        var result = await reports.GetDoctorAppointmentReportAsync(
            new DoctorAppointmentReportQuery(fromDate, toDate, mode, doctorId, departmentId, unitId, status, regNo, patientName, mobile), ct);
        return ReportExportHelper.ToFileResult(result, format, "doctor-appointment-report", ReportExportHelper.DateRangeLabel(fromDate, toDate), excel, pdf);
    }
}
