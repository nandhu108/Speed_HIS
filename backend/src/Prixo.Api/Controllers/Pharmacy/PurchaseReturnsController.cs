using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/purchase-returns")]
public class PurchaseReturnsController(PurchaseService purchase) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.PurchaseReturnView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PurchaseReturnListItemDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await purchase.GetPurchaseReturnsAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.PurchaseReturnView)]
    public async Task<ActionResult<ApiResponse<PurchaseReturnDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await purchase.GetPurchaseReturnByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.PurchaseReturnCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreatePurchaseReturnRequest request, CancellationToken ct)
    {
        var id = await purchase.CreatePurchaseReturnAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/purchase-returns/{id}", id, "Purchase return posted.");
    }
}
