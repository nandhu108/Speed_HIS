using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Mrd;

namespace Prixo.Api.Controllers.Mrd;

[Route("api/v1/mrd/retention-policies")]
public class RetentionPoliciesController(RetentionPolicyService policies) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.MrdRetentionView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RetentionPolicyDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await policies.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.MrdRetentionManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveRetentionPolicyRequest request, CancellationToken ct)
    {
        var id = await policies.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/mrd/retention-policies/{id}", id, "Retention policy created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.MrdRetentionManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveRetentionPolicyRequest request, CancellationToken ct)
    {
        await policies.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Retention policy updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.MrdRetentionManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await policies.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Retention policy deleted.");
    }
}
