using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Er;

public record RegisterEmergencyVisitRequest(long PatientId, ArrivalMode ArrivalMode, bool IsMlc);
public record CancelEmergencyVisitRequest(string Reason);

public record RecordTriageRequest(
    EsiLevel EsiLevel, string ChiefComplaint,
    decimal? TemperatureF, int? PulseRate, int? RespiratoryRate, int? BloodPressureSystolic, int? BloodPressureDiastolic, int? SpO2,
    string? Notes, long? AssignDoctorId);

public record TriageDto(
    long Id, DateTimeOffset TriagedAt, string TriagedByName, EsiLevel EsiLevel, string ChiefComplaint,
    decimal? TemperatureF, int? PulseRate, int? RespiratoryRate, int? BloodPressureSystolic, int? BloodPressureDiastolic, int? SpO2, string? Notes);

public record EmergencyVisitListItemDto(
    long Id, string VisitNumber, int TokenNumber, long PatientId, string PatientName, string PatientUhid, DateTimeOffset ArrivedAt,
    ArrivalMode ArrivalMode, EmergencyStatus Status, EsiLevel? EsiLevel, string? AssignedDoctorName, bool IsMlc,
    DispositionType? Disposition, bool AlreadyAdmitted);

public record EmergencyVisitDetailDto(
    long Id, string VisitNumber, int TokenNumber, long PatientId, string PatientName, string PatientUhid,
    DateTimeOffset ArrivedAt, ArrivalMode ArrivalMode, bool IsMlc, EmergencyStatus Status,
    long? AssignedDoctorId, string? AssignedDoctorName, string? CancelReason, TriageDto? Triage);

public class EmergencyVisitQuery
{
    public EmergencyStatus? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
