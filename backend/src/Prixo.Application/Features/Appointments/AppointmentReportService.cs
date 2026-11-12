using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Reports;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Appointments;

/// <summary>"Doctor-Appointment Report" — a standalone, date-range-filterable ad-hoc report over
/// `appointment.appointments`, exported via the shared <see cref="ReportExportHelper"/>. Global EF
/// query filters already scope by branch, matching every other report service.</summary>
public class AppointmentReportService(IAppDbContext db)
{
    public async Task<ReportResultDto> GetDoctorAppointmentReportAsync(
        DoctorAppointmentReportQuery query, CancellationToken ct = default)
    {
        var mode = (query.Mode ?? "summary").ToLowerInvariant();
        if (mode is not ("summary" or "detail"))
        {
            throw new DomainException($"Unknown report mode '{query.Mode}' — use 'summary' or 'detail'.");
        }

        var appointments = db.Appointments.AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .Include(a => a.Doctor).ThenInclude(d => d!.DoctorDepartments).ThenInclude(dd => dd.Department)
            .AsQueryable();
        if (query.FromDate is { } from) appointments = appointments.Where(a => a.AppointmentDate >= from);
        if (query.ToDate is { } to) appointments = appointments.Where(a => a.AppointmentDate <= to);
        if (query.DoctorId is { } doctorId) appointments = appointments.Where(a => a.DoctorId == doctorId);
        if (query.DepartmentId is { } departmentId) appointments = appointments.Where(a => a.Doctor!.DoctorDepartments.Any(dd => dd.DepartmentId == departmentId));
        if (query.Status is { } status) appointments = appointments.Where(a => a.Status == status);
        if (!string.IsNullOrWhiteSpace(query.RegNo)) appointments = appointments.Where(a => a.Patient!.Uhid.Contains(query.RegNo));
        if (!string.IsNullOrWhiteSpace(query.Mobile)) appointments = appointments.Where(a => a.Patient!.Mobile.Contains(query.Mobile));
        if (!string.IsNullOrWhiteSpace(query.PatientName))
        {
            var name = query.PatientName.Trim();
            appointments = appointments.Where(a => (a.Patient!.FirstName + " " + a.Patient.LastName).Contains(name));
        }
        if (query.UnitId is { } unitId)
        {
            var doctorIdsInUnit = await db.UnitDoctors.AsNoTracking()
                .Where(ud => ud.UnitId == unitId)
                .Select(ud => ud.DoctorId)
                .ToListAsync(ct);
            appointments = appointments.Where(a => doctorIdsInUnit.Contains(a.DoctorId));
        }

        var rows = await appointments.ToListAsync(ct);

        if (mode == "detail")
        {
            return await BuildDetailReportAsync(rows, ct);
        }

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("doctorName", "Doctor"), new("scheduledCount", "Scheduled"), new("checkedInCount", "Checked In"),
            new("completedCount", "Completed"), new("cancelledCount", "Cancelled"), new("noShowCount", "No-show"),
            new("totalCount", "Total"),
        ];
        var reportRows = rows
            .GroupBy(a => a.Doctor!.User!.FullName)
            .OrderBy(g => g.Key)
            .Select(g => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["doctorName"] = g.Key,
                ["scheduledCount"] = g.Count(a => a.Status == Domain.Enums.AppointmentStatus.Scheduled),
                ["checkedInCount"] = g.Count(a => a.Status == Domain.Enums.AppointmentStatus.CheckedIn),
                ["completedCount"] = g.Count(a => a.Status == Domain.Enums.AppointmentStatus.Completed),
                ["cancelledCount"] = g.Count(a => a.Status == Domain.Enums.AppointmentStatus.Cancelled),
                ["noShowCount"] = g.Count(a => a.Status == Domain.Enums.AppointmentStatus.NoShow),
                ["totalCount"] = g.Count(),
            }).ToList();
        return new ReportResultDto("Doctor-Appointment Report", columns, reportRows);
    }

    /// <summary>Patient-level appointment listing behind the summary counts — department/unit
    /// resolved through the doctor (same posture as OpReportService/IpReportService), bulk-loaded
    /// by id set rather than per-row.</summary>
    private async Task<ReportResultDto> BuildDetailReportAsync(List<Domain.Entities.Appointments.Appointment> rows, CancellationToken ct)
    {
        var doctorIds = rows.Select(a => a.DoctorId).Distinct().ToList();
        var unitNamesByDoctor = (await db.UnitDoctors.AsNoTracking()
            .Where(ud => doctorIds.Contains(ud.DoctorId))
            .Include(ud => ud.Unit)
            .ToListAsync(ct))
            .GroupBy(ud => ud.DoctorId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(ud => ud.Unit!.Name).Distinct()));

        IReadOnlyList<ReportColumnDto> columns =
        [
            new("appointmentDate", "Date"), new("tokenNumber", "Token"), new("patientUhid", "UHID"),
            new("patientName", "Patient"), new("mobile", "Mobile"), new("startTime", "Time"),
            new("doctorName", "Doctor"), new("department", "Department"), new("unit", "Unit"),
            new("status", "Status"), new("bookingType", "Booking Type"),
        ];
        var reportRows = rows
            .OrderBy(a => a.AppointmentDate).ThenBy(a => a.TokenNumber)
            .Select(a =>
            {
                var deptNames = a.Doctor?.DoctorDepartments.Select(dd => dd.Department!.Name).Distinct().ToList() ?? [];
                return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
                {
                    ["appointmentDate"] = a.AppointmentDate.ToString("yyyy-MM-dd"),
                    ["tokenNumber"] = a.TokenNumber,
                    ["patientUhid"] = a.Patient!.Uhid,
                    ["patientName"] = a.Patient.FirstName + " " + a.Patient.LastName,
                    ["mobile"] = a.Patient.Mobile,
                    ["startTime"] = a.StartTime.ToString("HH:mm"),
                    ["doctorName"] = a.Doctor?.User?.FullName ?? "-",
                    ["department"] = deptNames.Count > 0 ? string.Join(", ", deptNames) : "-",
                    ["unit"] = unitNamesByDoctor.GetValueOrDefault(a.DoctorId, "-"),
                    ["status"] = a.Status.ToString(),
                    ["bookingType"] = a.BookingType.ToString(),
                };
            }).ToList();
        return new ReportResultDto("Doctor-Appointment Report (Detail)", columns, reportRows);
    }
}
