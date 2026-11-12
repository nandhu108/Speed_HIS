using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Patients.Abdm;

namespace Prixo.Api.Controllers.Abdm;

/// <summary>
/// ABDM Milestone M2 — Health Information Provider (HIP) API endpoints.
/// Exposes care context discovery, care context linkage, and health data sharing.
/// </summary>
[Route("api/v1/abdm/hip")]
public class AbdmHipController(AbdmHipService hip) : ApiControllerBase
{
    /// <summary>List linked and discoverable care contexts for a patient.</summary>
    [HttpGet("care-contexts/{patientId:long}")]
    [HasPermission(PermissionRegistry.AbdmHipView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CareContextDto>>>> GetCareContexts(
        long patientId, CancellationToken ct) =>
        OkResponse(await hip.GetCareContextsAsync(patientId, ct));

    /// <summary>Sync and discover all latest encounters (OP/IP/Lab) as care contexts.</summary>
    [HttpPost("care-contexts/{patientId:long}/sync")]
    [HasPermission(PermissionRegistry.AbdmHipManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CareContextDto>>>> SyncCareContexts(
        long patientId, CancellationToken ct) =>
        OkResponse(await hip.DiscoverAndSyncCareContextsAsync(patientId, ct), "Care contexts refreshed.");

    /// <summary>Link selected or all unlinked care contexts with ABDM.</summary>
    [HttpPost("care-contexts/{patientId:long}/link")]
    [HasPermission(PermissionRegistry.AbdmHipManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CareContextDto>>>> LinkCareContexts(
        long patientId, [FromBody] LinkCareContextsRequest request, CancellationToken ct) =>
        OkResponse(await hip.LinkCareContextsAsync(patientId, request, ct), "Care contexts linked with ABDM.");

    /// <summary>Share FHIR health data bundle for a care context in response to a consent artifact.</summary>
    [HttpPost("share-data")]
    [HasPermission(PermissionRegistry.AbdmHipManage)]
    public async Task<ActionResult<ApiResponse<DataTransferLogDto>>> ShareData(
        [FromBody] ShareDataRequest request, CancellationToken ct) =>
        OkResponse(await hip.ShareHealthDataAsync(request, ct), "Health data bundle shared successfully.");

    /// <summary>Get recent health data transfer logs (HIP audit trail).</summary>
    [HttpGet("transfers")]
    [HasPermission(PermissionRegistry.AbdmHipView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DataTransferLogDto>>>> GetTransfers(
        [FromQuery] int take = 50, CancellationToken ct = default) =>
        OkResponse(await hip.GetDataTransferLogsAsync(take, ct));
}
