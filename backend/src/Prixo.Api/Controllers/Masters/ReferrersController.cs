using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Referrers;

namespace Prixo.Api.Controllers.Masters;

public class ReferrersController(ReferrerService referrers) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.ReferrerView)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReferrerDto>>>> GetPaged(
        [FromQuery] PagedQuery query, CancellationToken ct) =>
        OkResponse(await referrers.GetPagedAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.ReferrerManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveReferrerRequest request, CancellationToken ct)
    {
        var id = await referrers.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/referrers/{id}", id, "Referrer created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.ReferrerManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveReferrerRequest request, CancellationToken ct)
    {
        await referrers.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Referrer updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.ReferrerManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await referrers.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Referrer deleted.");
    }
}
