using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Ipd;

public record AdmitPatientRequest(
    long PatientId, long PrimaryConsultantId, long BedId, AdmissionType AdmissionType,
    string? ProvisionalDiagnosis, long? ReferringOpVisitId, long? ReferringEmergencyVisitId,
    IReadOnlyList<long> SecondaryConsultantIds,
    long? ConsentTemplateId, string? ConsentSignedByName, string? ConsentRelationshipToPatient,
    decimal InitialDepositAmount, long? DepositPaymentModeId, string? DepositReferenceNumber,
    long? AdmissionRequestId = null, long? DepartmentId = null);

public record AdmissionListItemDto(
    long Id, string AdmissionNumber, DateTimeOffset AdmissionDate, long PatientId, string PatientUhid,
    string PatientName, AdmissionType AdmissionType, AdmissionStatus Status, string PrimaryConsultantName,
    string WardName, string RoomName, string BedCode);

public record AdmissionConsultantDto(
    long Id, long DoctorId, string DoctorName, ConsultantRole Role, DateTimeOffset AssignedAt, DateTimeOffset? EndedAt, string? Notes);

public record BedAllocationDto(
    long Id, long BedId, string WardName, string RoomName, string BedCode, decimal BedChargePerDay,
    DateTimeOffset AllocatedAt, DateTimeOffset? ReleasedAt, string? TransferReason);

public record AdmissionDetailDto(
    long Id, string AdmissionNumber, DateTimeOffset AdmissionDate, long PatientId, string PatientUhid,
    string PatientName, string PatientGender, string PatientAgeDisplay, AdmissionType AdmissionType,
    string? ProvisionalDiagnosis, long PrimaryConsultantId, string PrimaryConsultantName,
    long? ReferringOpVisitId, string? ReferringOpVisitNumber, long? ReferringEmergencyVisitId,
    string? ReferringEmergencyVisitNumber, AdmissionStatus Status, long CurrentBedId, string WardName,
    string RoomName, string BedCode, DateTimeOffset? DischargeRequestedAt, DischargeType? DischargeType,
    string? DischargeTypeNotes, DateTimeOffset? DischargedAt, string? CancelReason, decimal? CreditLimit,
    IReadOnlyList<AdmissionConsultantDto> Consultants, IReadOnlyList<BedAllocationDto> BedAllocations,
    long? DepartmentId = null, string? DepartmentName = null);

public record AdmissionQuery(long? PatientId, AdmissionStatus? Status, string? Search, int Page = 1, int PageSize = 20);

public record TransferBedRequest(long NewBedId, string? Reason);
public record TransferConsultantRequest(long NewPrimaryConsultantId, string? Notes);
public record AddSecondaryConsultantRequest(long DoctorId, string? Notes);
public record CancelAdmissionRequest(string Reason);
public record SetCreditLimitRequest(decimal? CreditLimit);
