using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Patients.Abdm;

namespace Prixo.Api.Controllers.Patients;

/// <summary>
/// ABDM / ABHA (Wave-I integration, milestone M1) — link, create or view a patient's ABHA.
/// The verify flow is a two-step OTP handshake: initiate → complete. All endpoints are
/// permission-gated; the underlying gateway is config-gated and returns a clear "not
/// configured" message until ABDM sandbox credentials are supplied.
/// </summary>
[Route("api/v1/patients/{patientId:long}/abha")]
public class AbdmController(AbdmService abdm) : ApiControllerBase
{
    /// <summary>The patient's linked ABHA, or null if none is linked.</summary>
    [HttpGet]
    [HasPermission(PermissionRegistry.AbhaView)]
    public async Task<ActionResult<ApiResponse<AbhaProfileDto?>>> Get(long patientId, CancellationToken ct) =>
        OkResponse(await abdm.GetProfileAsync(patientId, ct));

    /// <summary>Step 1 — request an OTP against an Aadhaar / mobile / existing ABHA.</summary>
    [HttpPost("verify/initiate")]
    [HasPermission(PermissionRegistry.AbhaManage)]
    public async Task<ActionResult<ApiResponse<AbhaVerificationSessionDto>>> Initiate(
        long patientId, InitiateAbhaVerificationRequest request, CancellationToken ct) =>
        OkResponse(await abdm.InitiateAsync(patientId, request, ct), "OTP requested.");

    /// <summary>Step 2 — submit the OTP to verify and link/create the ABHA.</summary>
    [HttpPost("verify/complete")]
    [HasPermission(PermissionRegistry.AbhaManage)]
    public async Task<ActionResult<ApiResponse<AbhaProfileDto>>> Complete(
        long patientId, CompleteAbhaVerificationRequest request, CancellationToken ct) =>
        OkResponse(await abdm.CompleteAsync(patientId, request, ct), "ABHA linked.");

    /// <summary>Scan & Share — link ABHA directly by scanning patient's ABDM QR code.</summary>
    [HttpPost("scan-and-share")]
    [HasPermission(PermissionRegistry.AbhaManage)]
    public async Task<ActionResult<ApiResponse<AbhaProfileDto>>> ScanAndShare(
        long patientId, ScanAndShareQrRequest request, CancellationToken ct) =>
        OkResponse(await abdm.ScanAndShareAsync(patientId, request, ct), "ABHA linked via Scan & Share.");

    /// <summary>Remove the patient's ABHA link (reason-coded, audited).</summary>
    [HttpPost("unlink")]
    [HasPermission(PermissionRegistry.AbhaManage)]
    public async Task<ActionResult<ApiResponse>> Unlink(
        long patientId, UnlinkAbhaRequest request, CancellationToken ct)
    {
        await abdm.UnlinkAsync(patientId, request, ct);
        return Ok(ApiResponse.Ok("ABHA unlinked."));
    }
}
