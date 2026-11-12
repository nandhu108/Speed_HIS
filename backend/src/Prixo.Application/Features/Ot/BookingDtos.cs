using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Ot;

public record CreateBookingRequest(
    long AdmissionId, long TheatreId, string ProcedureName, long SurgeonId, long? AnaesthetistId,
    DateTimeOffset ScheduledStart, DateTimeOffset ScheduledEnd, string? Notes);

public record RescheduleBookingRequest(DateTimeOffset ScheduledStart, DateTimeOffset ScheduledEnd);
/// <summary>"OT Patient Update" — corrects a mis-selected patient/admission on a booking created
/// against the wrong one; re-points both the denormalized PatientId and AdmissionId.</summary>
public record UpdateBookingPatientRequest(long AdmissionId);
public record CancelBookingRequest(string Reason);
public record PostponeBookingRequest(string Reason);
public record VerifyConsentRequest(long PatientConsentId);

public record AddTeamMemberRequest(SurgeryTeamRole Role, long? DoctorId, string? StaffName);

public record SurgeryTeamMemberDto(long Id, SurgeryTeamRole Role, long? DoctorId, string? DoctorName, string? StaffName);

public record BookingListItemDto(
    long Id, string BookingNumber, string PatientName, string TheatreName, string ProcedureName,
    string SurgeonName, string? AnaesthetistName, DateTimeOffset ScheduledStart, DateTimeOffset ScheduledEnd, SurgeryStatus Status);

public record BookingDetailDto(
    long Id, string BookingNumber, long PatientId, string PatientName, long AdmissionId, string AdmissionNumber,
    long TheatreId, string TheatreName, string ProcedureName,
    long SurgeonId, string SurgeonName, long? AnaesthetistId, string? AnaesthetistName,
    DateTimeOffset ScheduledStart, DateTimeOffset ScheduledEnd, DateTimeOffset? ActualStart, DateTimeOffset? ActualEnd,
    SurgeryStatus Status, string? StatusReason,
    long? PatientConsentId, DateTimeOffset? ConsentVerifiedAt, string? Notes,
    IReadOnlyList<SurgeryTeamMemberDto> TeamMembers);

public class BookingQuery
{
    public SurgeryStatus? Status { get; set; }
    public long? TheatreId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public record OtRegisterRowDto(
    long BookingId, string BookingNumber, string PatientName, string ProcedureName, string TheatreName,
    string SurgeonName, string? AnaesthetistName, DateTimeOffset ScheduledStart, DateTimeOffset? ActualStart,
    DateTimeOffset? ActualEnd, SurgeryStatus Status);
