using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Ipd;

namespace Prixo.Api.Controllers.Ipd;

[Route("api/v1/ipd/admission-requests")]
public class AdmissionRequestsController(AdmissionRequestService requests) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.AdmissionRequestView)]
    public async Task<ActionResult<ApiResponse<PagedResult<AdmissionRequestDto>>>> GetPaged(
        [FromQuery] AdmissionRequestQuery query, CancellationToken ct) =>
        OkResponse(await requests.GetPagedAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.AdmissionRequestCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateAdmissionRequestRequest request, CancellationToken ct)
    {
        var id = await requests.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/ipd/admission-requests/{id}", id, "Admission request raised.");
    }

    [HttpPost("{id:long}/approve")]
    [HasPermission(PermissionRegistry.AdmissionRequestDecide)]
    public async Task<ActionResult<ApiResponse>> Approve(long id, CancellationToken ct)
    {
        await requests.ApproveAsync(id, ct);
        return OkResponse<object?>(null, "Admission request approved.");
    }

    [HttpPost("{id:long}/reject")]
    [HasPermission(PermissionRegistry.AdmissionRequestDecide)]
    public async Task<ActionResult<ApiResponse>> Reject(long id, RejectAdmissionRequestRequest request, CancellationToken ct)
    {
        await requests.RejectAsync(id, request, ct);
        return OkResponse<object?>(null, "Admission request rejected.");
    }
}
