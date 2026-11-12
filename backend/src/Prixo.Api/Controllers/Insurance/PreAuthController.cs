using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Insurance;

namespace Prixo.Api.Controllers.Insurance;

[Route("api/v1/insurance/pre-auth-requests")]
public class PreAuthController(PreAuthService preAuth) : ApiControllerBase
{
    [HttpGet("pending")]
    [HasPermission(PermissionRegistry.InsurancePreAuthView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PreAuthRequestDto>>>> GetPending(CancellationToken ct) =>
        OkResponse(await preAuth.GetPendingAsync(ct));

    /// <summary>Approved/PartiallyApproved pre-auths — lets staff find one again to raise or decide an "enhancement" against it, since GetPending only ever shows Requested rows.</summary>
    [HttpGet("approved")]
    [HasPermission(PermissionRegistry.InsurancePreAuthView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PreAuthRequestDto>>>> GetApproved(CancellationToken ct) =>
        OkResponse(await preAuth.GetApprovedAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.InsurancePreAuthView)]
    public async Task<ActionResult<ApiResponse<PreAuthRequestDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await preAuth.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.InsurancePreAuthCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Raise(RaisePreAuthRequest request, CancellationToken ct)
    {
        var id = await preAuth.RaiseAsync(request, ct);
        return CreatedResponse($"/api/v1/insurance/pre-auth-requests/{id}", id, "Pre-authorization requested.");
    }

    [HttpPost("{id:long}/decide")]
    [HasPermission(PermissionRegistry.InsurancePreAuthApprove)]
    public async Task<ActionResult<ApiResponse>> Decide(long id, DecidePreAuthRequest request, CancellationToken ct)
    {
        await preAuth.DecideAsync(id, request, ct);
        return OkResponse<object?>(null, request.Approve ? "Pre-authorization approved." : "Pre-authorization rejected.");
    }

    [HttpGet("{id:long}/enhancements")]
    [HasPermission(PermissionRegistry.InsurancePreAuthView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PreAuthEnhancementDto>>>> GetEnhancements(
        long id, CancellationToken ct) =>
        OkResponse(await preAuth.GetEnhancementsAsync(id, ct));

    [HttpPost("{id:long}/enhancements")]
    [HasPermission(PermissionRegistry.InsurancePreAuthCreate)]
    public async Task<ActionResult<ApiResponse<long>>> RequestEnhancement(
        long id, RequestEnhancementRequest request, CancellationToken ct)
    {
        var enhancementId = await preAuth.RequestEnhancementAsync(id, request, ct);
        return CreatedResponse(
            $"/api/v1/insurance/pre-auth-requests/{id}/enhancements/{enhancementId}", enhancementId,
            "Enhancement requested.");
    }

    [HttpPost("enhancements/{enhancementId:long}/decide")]
    [HasPermission(PermissionRegistry.InsurancePreAuthApprove)]
    public async Task<ActionResult<ApiResponse>> DecideEnhancement(
        long enhancementId, DecideEnhancementRequest request, CancellationToken ct)
    {
        await preAuth.DecideEnhancementAsync(enhancementId, request, ct);
        return OkResponse<object?>(null, request.Approve ? "Enhancement approved." : "Enhancement rejected.");
    }
}
