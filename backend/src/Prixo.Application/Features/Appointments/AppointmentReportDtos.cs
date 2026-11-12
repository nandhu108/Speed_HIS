using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Appointments;

/// <summary>"Doctor-Appointment Report" — per-doctor Scheduled/CheckedIn/Completed/Cancelled/
/// No-show breakdown (Summary) or a patient-level listing (Detail) over a date range.</summary>
public record DoctorAppointmentReportQuery(
    DateOnly? FromDate, DateOnly? ToDate, string? Mode = "summary",
    long? DoctorId = null, long? DepartmentId = null, long? UnitId = null,
    AppointmentStatus? Status = null, string? RegNo = null, string? PatientName = null, string? Mobile = null);
