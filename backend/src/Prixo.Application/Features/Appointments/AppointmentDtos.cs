using Prixo.Application.Common.Models;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Appointments;

public record SlotDto(TimeOnly StartTime, TimeOnly EndTime, bool IsAvailable);

public record AppointmentListItemDto(
    long Id, long PatientId, string PatientUhid, string PatientName, long DoctorId, string DoctorName,
    DateOnly AppointmentDate, TimeOnly StartTime, TimeOnly EndTime, int TokenNumber,
    AppointmentStatus Status, AppointmentBookingType BookingType,
    long? AppointmentTypeId, string? AppointmentTypeName,
    DateTimeOffset? SkippedAt, long? EmergencyVisitId,
    ConsultationMode ConsultationMode, string? TeleconsultLink);

public record AppointmentDetailDto(
    long Id, long PatientId, string PatientUhid, string PatientName, string PatientMobile,
    long DoctorId, string DoctorName, DateOnly AppointmentDate, TimeOnly StartTime, TimeOnly EndTime,
    int TokenNumber, AppointmentStatus Status, AppointmentBookingType BookingType, string? Notes,
    string? CancelReason, DateTimeOffset? CancelledAt, DateTimeOffset? CheckedInAt,
    DateTimeOffset? NoShowMarkedAt, long? RescheduledFromId,
    long? AppointmentTypeId, string? AppointmentTypeName,
    DateTimeOffset? SkippedAt, int SkipCount,
    long? TransferredFromDoctorId, string? TransferredFromDoctorName, DateTimeOffset? TransferredAt,
    long? EmergencyVisitId,
    ConsultationMode ConsultationMode, string? TeleconsultLink);

public record BookAppointmentRequest(
    long PatientId, long DoctorId, DateOnly AppointmentDate, TimeOnly StartTime,
    AppointmentBookingType BookingType, string? Notes, long? AppointmentTypeId = null,
    ConsultationMode ConsultationMode = ConsultationMode.InPerson, string? TeleconsultLink = null);

public record RescheduleAppointmentRequest(DateOnly NewDate, TimeOnly NewStartTime, string Reason);

public record CancelAppointmentRequest(string Reason);

/// <summary>"Token Transfer" — reassign a token to a different doctor; a fresh token number is
/// minted from the new doctor's per-day sequence.</summary>
public record TransferTokenRequest(long NewDoctorId, string? Reason);

/// <summary>"Tele-consulting" — set/replace the external meeting link after booking (e.g. once a
/// Zoom/Meet link has actually been generated). Only valid when ConsultationMode is TeleConsult.</summary>
public record SaveTeleconsultLinkRequest(string? TeleconsultLink);

public class AppointmentQuery : PagedQuery
{
    public long? DoctorId { get; set; }
    public long? PatientId { get; set; }
    public DateOnly? Date { get; set; }
    public AppointmentStatus? Status { get; set; }
    public long? EmergencyVisitId { get; set; }
}

public record QueueEntryDto(
    long AppointmentId, int TokenNumber, long PatientId, string PatientName, string PatientUhid,
    long DoctorId, string DoctorName, TimeOnly StartTime, AppointmentStatus Status,
    DateTimeOffset? SkippedAt, long? EmergencyVisitId, ConsultationMode ConsultationMode);

// ── Doctors On Duty ─────────────────────────────────────────────────────────
public record DoctorOnDutyWindowDto(TimeOnly StartTime, TimeOnly EndTime);

/// <summary>One row in the "Doctors On Duty" board — purely a projection of the existing
/// DoctorSchedule/ScheduleOverride data (Phase 4's slot-engine availability windows) across every
/// doctor for a date, not a new duty-roster entity.</summary>
public record DoctorOnDutyDto(
    long DoctorId, string DoctorName, string? SpecialtyName, IReadOnlyList<string> DepartmentNames,
    bool IsOnDuty, IReadOnlyList<DoctorOnDutyWindowDto> Windows, string? LeaveReason);
