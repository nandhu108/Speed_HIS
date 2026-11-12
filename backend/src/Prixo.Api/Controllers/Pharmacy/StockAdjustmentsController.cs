using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/adjustments")]
public class StockAdjustmentsController(StockService stock) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.StockAdjust)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockAdjustmentListItemDto>>>> GetPending(CancellationToken ct) =>
        OkResponse(await stock.GetPendingAdjustmentsAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.StockAdjust)]
    public async Task<ActionResult<ApiResponse<StockAdjustmentDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await stock.GetAdjustmentByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.StockAdjust)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateStockAdjustmentRequest request, CancellationToken ct)
    {
        var id = await stock.CreateAdjustmentAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/adjustments/{id}", id, "Stock adjustment requested.");
    }

    [HttpPost("{id:long}/decide")]
    [HasPermission(PermissionRegistry.StockApprove)]
    public async Task<ActionResult<ApiResponse>> Decide(long id, DecideStockAdjustmentRequest request, CancellationToken ct)
    {
        await stock.DecideAdjustmentAsync(id, request, ct);
        return OkResponse<object?>(null, request.Approve ? "Stock adjustment approved." : "Stock adjustment rejected.");
    }
}
