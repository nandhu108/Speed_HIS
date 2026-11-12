using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Mrd;

namespace Prixo.Api.Controllers.Mrd;

[Route("api/v1/mrd/destruction-register")]
public class DestructionRegisterController(DestructionRegisterService register) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.MrdDestructionView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DestructionRegisterEntryDto>>>> GetAll([FromQuery] DestructionRegisterQuery query, CancellationToken ct) =>
        OkResponse(await register.GetAllAsync(query, ct));

    [HttpGet("eligible")]
    [HasPermission(PermissionRegistry.MrdDestructionView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EligibleFileDto>>>> GetEligible([FromQuery] long retentionPolicyId, CancellationToken ct) =>
        OkResponse(await register.GetEligibleAsync(retentionPolicyId, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.MrdDestructionManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateDestructionEntryRequest request, CancellationToken ct)
    {
        var id = await register.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/mrd/destruction-register/{id}", id, "Flagged for destruction.");
    }

    [HttpPost("{id:long}/approve")]
    [HasPermission(PermissionRegistry.MrdDestructionManage)]
    public async Task<ActionResult<ApiResponse>> Approve(long id, CancellationToken ct)
    {
        await register.ApproveAsync(id, ct);
        return OkResponse<object?>(null, "Destruction approved.");
    }

    [HttpPost("{id:long}/destroy")]
    [HasPermission(PermissionRegistry.MrdDestructionManage)]
    public async Task<ActionResult<ApiResponse>> Destroy(long id, DestroyRequest request, CancellationToken ct)
    {
        await register.DestroyAsync(id, request, ct);
        return OkResponse<object?>(null, "Record file destroyed.");
    }
}
