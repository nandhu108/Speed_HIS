using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Insurance;

public record PreAuthRequestDto(
    long Id, string PreAuthNumber, long PatientId, string PatientUhid, string PatientName,
    long PolicyId, string PolicyNumber, string InsuranceCompanyName,
    PreAuthTreatmentType TreatmentType, long? OpVisitId, long? AdmissionId,
    decimal EstimatedAmount, string Diagnosis,
    string RequestedByUserName, DateTimeOffset RequestedAt,
    PreAuthStatus Status, decimal? ApprovedAmount,
    string? DecidedByUserName, DateTimeOffset? DecidedAt, string? RejectionReason);

public record RaisePreAuthRequest(
    long PatientId, long PolicyId, PreAuthTreatmentType TreatmentType,
    long? OpVisitId, long? AdmissionId, decimal EstimatedAmount, string Diagnosis);

public record DecidePreAuthRequest(bool Approve, decimal? ApprovedAmount, string? RejectionReason);

public record PreAuthEnhancementDto(
    long Id, long PreAuthRequestId, decimal RequestedAmount, string Reason,
    string RequestedByUserName, DateTimeOffset RequestedAt,
    PreAuthEnhancementStatus Status, string? DecidedByUserName, DateTimeOffset? DecidedAt, string? RejectionReason);

public record RequestEnhancementRequest(decimal RequestedAmount, string Reason);
public record DecideEnhancementRequest(bool Approve, string? RejectionReason);

/// <summary>
/// BillPaidAmount/BillBalanceAmount are the bill's *current* totals (as of the read, not
/// snapshotted at submission) — surfaced so insurance staff can see whether the counter has
/// already collected cash against this bill (partially or in full) before/while a claim is
/// worked, closing half of the cash-vs-cashless awareness gap; see BillDetailDto.InsuranceStatus
/// for the other half (the cashier's own view). Advisory only, same posture there.
/// </summary>
public record ClaimDto(
    long Id, string ClaimNumber, long PatientId, string PatientUhid, string PatientName,
    long PolicyId, string PolicyNumber, string InsuranceCompanyName, long? PreAuthRequestId,
    long BillId, string BillNumber, decimal ClaimAmount, decimal? ApprovedAmount, ClaimStatus Status,
    string? SubmittedByUserName, DateTimeOffset? SubmittedAt,
    string? DecidedByUserName, DateTimeOffset? DecidedAt,
    long? DenialReasonId, string? DenialReasonName, string? DenialNotes,
    decimal? SettledAmount, decimal? DeductionAmount, string? SettlementReferenceNumber, DateTimeOffset? SettledAt,
    decimal BillPaidAmount, decimal BillBalanceAmount);

public record SubmitClaimRequest(
    long PatientId, long PolicyId, long? PreAuthRequestId, long BillId, decimal ClaimAmount);

public record UpdateClaimStatusRequest(
    ClaimStatus Status, decimal? ApprovedAmount, long? DenialReasonId, string? DenialNotes);

public record RecordClaimSettlementRequest(
    decimal SettledAmount, decimal? DeductionAmount, long PaymentModeId, string? SettlementReferenceNumber);

public record ClaimDocumentDto(
    long Id, long DocumentTypeId, string DocumentTypeName, string FileName,
    string ContentType, long SizeBytes, DateTimeOffset UploadedAt);
public record AddClaimDocumentRequest(long DocumentTypeId);
