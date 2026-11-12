using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/purchase-requests")]
public class PurchaseRequestsController(PurchaseService purchase) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.PurchaseRequestView)]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseRequestListItemDto>>>> GetPaged(
        [FromQuery] PurchaseRequestQuery query, CancellationToken ct) =>
        OkResponse(await purchase.GetPurchaseRequestsAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.PurchaseRequestView)]
    public async Task<ActionResult<ApiResponse<PurchaseRequestDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await purchase.GetPurchaseRequestByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.PurchaseRequestCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreatePurchaseRequestRequest request, CancellationToken ct)
    {
        var id = await purchase.CreatePurchaseRequestAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/purchase-requests/{id}", id, "Purchase request created.");
    }

    [HttpPost("{id:long}/decide")]
    [HasPermission(PermissionRegistry.PurchaseRequestApprove)]
    public async Task<ActionResult<ApiResponse>> Decide(long id, DecidePurchaseRequestRequest request, CancellationToken ct)
    {
        await purchase.DecidePurchaseRequestAsync(id, request, ct);
        return OkResponse<object?>(null, request.Approve ? "Purchase request approved." : "Purchase request rejected.");
    }
}
