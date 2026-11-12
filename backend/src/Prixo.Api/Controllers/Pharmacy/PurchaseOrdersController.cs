using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/purchase-orders")]
public class PurchaseOrdersController(PurchaseService purchase) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.PurchaseOrderView)]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseOrderListItemDto>>>> GetPaged(
        [FromQuery] PurchaseOrderQuery query, CancellationToken ct) =>
        OkResponse(await purchase.GetPurchaseOrdersAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.PurchaseOrderView)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await purchase.GetPurchaseOrderByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.PurchaseOrderCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var id = await purchase.CreatePurchaseOrderAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/purchase-orders/{id}", id, "Purchase order created.");
    }

    [HttpPost("{id:long}/approve")]
    [HasPermission(PermissionRegistry.PurchaseOrderApprove)]
    public async Task<ActionResult<ApiResponse>> Approve(long id, CancellationToken ct)
    {
        await purchase.ApprovePurchaseOrderAsync(id, ct);
        return OkResponse<object?>(null, "Purchase order approved.");
    }
}
