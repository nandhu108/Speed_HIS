using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Patients.Abdm;

namespace Prixo.Api.Controllers.Abdm;

/// <summary>
/// ABDM Milestone M3 — Health Information User (HIU) API endpoints.
/// Exposes consent request initiation, consent status tracking, and longitudinal health records viewing.
/// </summary>
[Route("api/v1/abdm/hiu")]
public class AbdmHiuController(AbdmHiuService hiu) : ApiControllerBase
{
    /// <summary>Initiate a new ABDM consent request to access patient health records.</summary>
    [HttpPost("consent-requests")]
    [HasPermission(PermissionRegistry.AbdmHiuManage)]
    public async Task<ActionResult<ApiResponse<ConsentRequestDto>>> CreateConsentRequest(
        [FromBody] InitiateConsentRequestDto request, CancellationToken ct) =>
        OkResponse(await hiu.InitiateConsentRequestAsync(request, ct), "ABDM Consent request raised.");

    /// <summary>List consent requests for a patient.</summary>
    [HttpGet("consent-requests/patient/{patientId:long}")]
    [HasPermission(PermissionRegistry.AbdmHiuView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConsentRequestDto>>>> GetConsentRequests(
        long patientId, CancellationToken ct) =>
        OkResponse(await hiu.GetConsentRequestsAsync(patientId, ct));

    /// <summary>Check and update consent status from ABDM Gateway.</summary>
    [HttpPost("consent-requests/{id:long}/check-status")]
    [HasPermission(PermissionRegistry.AbdmHiuManage)]
    public async Task<ActionResult<ApiResponse<ConsentRequestDto>>> CheckStatus(
        long id, CancellationToken ct) =>
        OkResponse(await hiu.CheckConsentStatusAsync(id, ct), "Consent status updated.");

    /// <summary>Fetch and decrypt health records from other hospitals against a granted consent.</summary>
    [HttpPost("consent-requests/{id:long}/fetch-records")]
    [HasPermission(PermissionRegistry.AbdmHiuManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ExternalHealthRecordDto>>>> FetchRecords(
        long id, CancellationToken ct) =>
        OkResponse(await hiu.FetchExternalHealthRecordsAsync(id, ct), "External health records fetched successfully.");

    /// <summary>Get external longitudinal health records for a patient.</summary>
    [HttpGet("records/{patientId:long}")]
    [HasPermission(PermissionRegistry.AbdmHiuView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ExternalHealthRecordDto>>>> GetRecords(
        long patientId, CancellationToken ct) =>
        OkResponse(await hiu.GetExternalHealthRecordsAsync(patientId, ct));
}
