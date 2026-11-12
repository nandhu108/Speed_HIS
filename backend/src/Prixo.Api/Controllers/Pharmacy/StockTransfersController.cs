using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/transfers")]
public class StockTransfersController(StockService stock) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.StockTransfer)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockTransferListItemDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await stock.GetTransfersAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.StockTransfer)]
    public async Task<ActionResult<ApiResponse<StockTransferDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await stock.GetTransferByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.StockTransfer)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateStockTransferRequest request, CancellationToken ct)
    {
        var id = await stock.CreateTransferAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/transfers/{id}", id, "Stock transfer created.");
    }

    [HttpPost("{id:long}/dispatch")]
    [HasPermission(PermissionRegistry.StockTransfer)]
    public async Task<ActionResult<ApiResponse>> Dispatch(long id, CancellationToken ct)
    {
        await stock.DispatchTransferAsync(id, ct);
        return OkResponse<object?>(null, "Transfer dispatched.");
    }

    [HttpPost("{id:long}/receive")]
    [HasPermission(PermissionRegistry.StockReceive)]
    public async Task<ActionResult<ApiResponse>> Receive(long id, CancellationToken ct)
    {
        await stock.ReceiveTransferAsync(id, ct);
        return OkResponse<object?>(null, "Transfer received.");
    }
}
