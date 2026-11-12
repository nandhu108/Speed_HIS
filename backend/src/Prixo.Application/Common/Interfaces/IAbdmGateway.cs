using Prixo.Domain.Enums;

namespace Prixo.Application.Common.Interfaces;

/// <summary>
/// The seam to the ABDM (Ayushman Bharat Digital Mission) Gateway for:
/// - Milestone M1: ABHA creation, verification, QR scan & linkage.
/// - Milestone M2: Health Information Provider (HIP) care-context linking and FHIR data sharing.
/// - Milestone M3: Health Information User (HIU) consent requests & external health records retrieval.
/// </summary>
public interface IAbdmGateway
{
    /// <summary>False until ABDM credentials + base URL are configured; gates live external calls.</summary>
    bool IsConfigured { get; }

    // ── Milestone M1: ABHA & Identity ──────────────────────────────────────────
    Task<AbdmOtpChallenge> RequestOtpAsync(AbdmVerificationMethod method, string identifier, CancellationToken ct = default);
    Task<AbdmProfile> VerifyOtpAsync(string abdmTxnId, string otp, CancellationToken ct = default);
    Task<AbdmProfile> ParseQrPayloadAsync(string qrText, CancellationToken ct = default);

    // ── Milestone M2: Health Information Provider (HIP) ────────────────────────
    Task<bool> NotifyCareContextLinkedAsync(string hipId, string abhaNumber, string patientRef, IEnumerable<string> careContextRefs, CancellationToken ct = default);
    Task<bool> ShareHealthDataBundleAsync(string transactionId, string consentArtifactId, string recipientHiuId, string fhirBundleJson, CancellationToken ct = default);

    // ── Milestone M3: Health Information User (HIU) ────────────────────────────
    Task<string> SendConsentRequestAsync(AbdmConsentRequestParams request, CancellationToken ct = default);
    Task<AbdmConsentStatusCheckResult> CheckConsentStatusAsync(string consentRequestId, CancellationToken ct = default);
    Task<IReadOnlyList<AbdmExternalRecordItem>> FetchExternalHealthDataAsync(string consentArtifactId, CancellationToken ct = default);

    // ── Diagnostic & Connection ────────────────────────────────────────────────
    Task<AbdmGatewayStatus> CheckGatewayHealthAsync(CancellationToken ct = default);
}

/// <summary>ABDM's response to an OTP request — the transaction to carry into verification.</summary>
public record AbdmOtpChallenge(string TxnId, string? MaskedTarget);

/// <summary>The ABHA identity ABDM returns after a successful OTP verification or QR scan.</summary>
public record AbdmProfile(
    string AbhaNumber,
    string? AbhaAddress,
    string? Name,
    string? Gender,
    int? YearOfBirth,
    bool KycVerified);

public record AbdmConsentRequestParams(
    string PatientAbhaNumber,
    string? PatientAbhaAddress,
    string Purpose,
    IReadOnlyList<AbdmHiType> HiTypes,
    DateTimeOffset DateFrom,
    DateTimeOffset DateTo,
    DateTimeOffset ExpiryAt,
    string RequesterName);

public record AbdmConsentStatusCheckResult(
    string ConsentRequestId,
    AbdmConsentStatus Status,
    string? ConsentArtifactId,
    DateTimeOffset? UpdatedAt);

public record AbdmExternalRecordItem(
    AbdmHiType HiType,
    string SourceFacilityName,
    string SourceFacilityHipId,
    DateTimeOffset RecordDate,
    string? DoctorName,
    string Title,
    string ClinicalSummary,
    string FhirBundleJson);

public record AbdmGatewayStatus(
    bool Online,
    string GatewayUrl,
    string Message,
    DateTimeOffset CheckedAt);
