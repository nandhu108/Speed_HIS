using FluentValidation;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Patients.Abdm;

// ── Milestone M1: ABHA Registration & Verification ─────────────────────────

/// <summary>Step 1 — start ABHA verification/linkage by requesting an OTP.</summary>
public record InitiateAbhaVerificationRequest(AbdmVerificationMethod Method, string Identifier);

/// <summary>Server-side session handle for the OTP challenge (opaque to the client) + a masked target to show.</summary>
public record AbhaVerificationSessionDto(long SessionId, string? MaskedTarget, DateTimeOffset ExpiresAt);

/// <summary>Step 2 — complete verification by submitting the OTP against the session.</summary>
public record CompleteAbhaVerificationRequest(long SessionId, string Otp);

/// <summary>Scan patient's ABDM QR code from ABHA PHR app (Scan-and-Share counter flow).</summary>
public record ScanAndShareQrRequest(string QrPayload);

/// <summary>A patient's linked ABHA, as surfaced to the front desk and clinical views.</summary>
public record AbhaProfileDto(
    long PatientId,
    string AbhaNumber,
    string? AbhaAddress,
    AbhaLinkStatus Status,
    AbdmVerificationMethod VerificationMethod,
    bool KycVerified,
    string? LinkedName,
    string? LinkedGender,
    int? LinkedYearOfBirth,
    DateTimeOffset? VerifiedAt);

/// <summary>Remove a patient's ABHA link (separately permissioned) — e.g. wrong ABHA linked.</summary>
public record UnlinkAbhaRequest(string Reason);

// ── Milestone M2: Health Information Provider (HIP) ────────────────────────

public record CareContextDto(
    long Id,
    long PatientId,
    string PatientReference,
    string AbhaNumber,
    AbdmCareContextType ContextType,
    string ReferenceNumber,
    string Display,
    bool IsLinked,
    DateTimeOffset? LinkedAt,
    DateTimeOffset CreatedAt);

public record LinkCareContextsRequest(
    IReadOnlyList<long>? CareContextIds,
    bool LinkAllUnlinked = false);

public record ShareDataRequest(
    string ConsentArtifactId,
    string CareContextReference,
    AbdmHiType HiType,
    string RecipientHiuId);

public record DataTransferLogDto(
    long Id,
    string TransactionId,
    string ConsentArtifactId,
    string CareContextReference,
    AbdmHiType HiType,
    string RecipientHiuId,
    AbdmTransferStatus Status,
    DateTimeOffset TransferDate,
    string? FhirBundleJson,
    string? ErrorMessage);

// ── Milestone M3: Health Information User (HIU) ────────────────────────────

public record InitiateConsentRequestDto(
    long PatientId,
    string Purpose,
    IReadOnlyList<AbdmHiType> HiTypes,
    DateTimeOffset DateFrom,
    DateTimeOffset DateTo,
    DateTimeOffset ExpiryAt);

public record ConsentRequestDto(
    long Id,
    long PatientId,
    string ConsentRequestId,
    string? ConsentArtifactId,
    string AbhaNumber,
    string? AbhaAddress,
    string Purpose,
    string HiTypes,
    DateTimeOffset DateFrom,
    DateTimeOffset DateTo,
    DateTimeOffset ExpiryAt,
    AbdmConsentStatus Status,
    DateTimeOffset? StatusUpdatedAt,
    string? RequesterDoctorName,
    DateTimeOffset CreatedAt);

public record ExternalHealthRecordDto(
    long Id,
    long PatientId,
    long? ConsentRequestId,
    AbdmHiType HiType,
    string SourceFacilityName,
    string SourceFacilityHipId,
    DateTimeOffset RecordDate,
    string? DoctorName,
    string Title,
    string ClinicalSummary,
    string FhirBundleJson,
    DateTimeOffset CreatedAt);

// ── Admin & Governance ─────────────────────────────────────────────────────

public record AbdmAdminStatusDto(
    bool MasterEnabled,
    bool M1Enabled,
    bool M2Enabled,
    bool M3Enabled,
    string GatewayUrl,
    string ClientId,
    string HipId,
    string HiuId,
    bool IsConfigured,
    int TotalAbhaLinked,
    int TotalCareContextsLinked,
    int TotalDataTransfers,
    int TotalConsentRequests,
    int TotalExternalRecords,
    DateTimeOffset CheckedAt);

public record UpdateAbdmAdminConfigRequest(
    bool MasterEnabled,
    bool M1Enabled,
    bool M2Enabled,
    bool M3Enabled,
    string? GatewayUrl,
    string? ClientId,
    string? ClientSecret,
    string? HipId,
    string? HiuId);

public record ToggleMilestoneRequest(string Milestone, bool Enabled);

// ── Validators ─────────────────────────────────────────────────────────────

public class InitiateAbhaVerificationRequestValidator : AbstractValidator<InitiateAbhaVerificationRequest>
{
    public InitiateAbhaVerificationRequestValidator()
    {
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(50)
            .WithMessage("Enter the Aadhaar number, registered mobile, or ABHA number/address.");
    }
}

public class CompleteAbhaVerificationRequestValidator : AbstractValidator<CompleteAbhaVerificationRequest>
{
    public CompleteAbhaVerificationRequestValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.Otp).NotEmpty().Matches(@"^\d{4,8}$").WithMessage("Enter the OTP you received.");
    }
}

public class ScanAndShareQrRequestValidator : AbstractValidator<ScanAndShareQrRequest>
{
    public ScanAndShareQrRequestValidator()
    {
        RuleFor(x => x.QrPayload).NotEmpty().WithMessage("QR code data cannot be empty.");
    }
}

public class UnlinkAbhaRequestValidator : AbstractValidator<UnlinkAbhaRequest>
{
    public UnlinkAbhaRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class InitiateConsentRequestDtoValidator : AbstractValidator<InitiateConsentRequestDto>
{
    public InitiateConsentRequestDtoValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.Purpose).NotEmpty().MaximumLength(50);
        RuleFor(x => x.HiTypes).NotEmpty().WithMessage("Select at least one health information type.");
        RuleFor(x => x.ExpiryAt).GreaterThan(x => x.DateFrom).WithMessage("Expiry must be in the future.");
    }
}

public class UpdateAbdmAdminConfigRequestValidator : AbstractValidator<UpdateAbdmAdminConfigRequest>
{
    public UpdateAbdmAdminConfigRequestValidator()
    {
        RuleFor(x => x.GatewayUrl).MaximumLength(200);
        RuleFor(x => x.ClientId).MaximumLength(100);
        RuleFor(x => x.HipId).MaximumLength(100);
        RuleFor(x => x.HiuId).MaximumLength(100);
    }
}
