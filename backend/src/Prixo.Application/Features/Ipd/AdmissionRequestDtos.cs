using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Ipd;

public record CreateAdmissionRequestRequest(
    long PatientId, long? RequestedConsultantId, long? WardId,
    DateOnly ExpectedAdmissionDate, string? Reason);

public record RejectAdmissionRequestRequest(string Reason);

public record AdmissionRequestDto(
    long Id, long PatientId, string PatientUhid, string PatientName,
    long? RequestedConsultantId, string? RequestedConsultantName,
    long? WardId, string? WardName, DateOnly ExpectedAdmissionDate, string? Reason,
    AdmissionRequestStatus Status, string? RejectionReason,
    string RequestedByName, DateTimeOffset CreatedAt,
    string? DecidedByName, DateTimeOffset? DecidedAt, long? ApprovedAdmissionId);

public record AdmissionRequestQuery(int Page = 1, int PageSize = 20, AdmissionRequestStatus? Status = null);
