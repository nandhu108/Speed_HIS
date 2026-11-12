using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/stock")]
public class StockController(StockService stock) : ApiControllerBase
{
    [HttpGet("batches")]
    [HasPermission(PermissionRegistry.StockView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BatchDto>>>> GetBatches(
        [FromQuery] long? drugId, [FromQuery] long? itemId, [FromQuery] long? storeId, [FromQuery] bool onlyInStock, CancellationToken ct) =>
        OkResponse(await stock.GetBatchesAsync(drugId, itemId, storeId, onlyInStock, ct));

    [HttpGet("batches/{batchId:long}/ledger")]
    [HasPermission(PermissionRegistry.StockView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockLedgerEntryDto>>>> GetLedger(long batchId, CancellationToken ct) =>
        OkResponse(await stock.GetLedgerAsync(batchId, ct));

    [HttpGet("alerts/near-expiry")]
    [HasPermission(PermissionRegistry.PharmacyReportView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NearExpiryBatchDto>>>> GetNearExpiry(
        [FromQuery] int withinDays, CancellationToken ct) =>
        OkResponse(await stock.GetNearExpiryAsync(withinDays <= 0 ? 90 : withinDays, ct));

    [HttpGet("alerts/low-stock")]
    [HasPermission(PermissionRegistry.PharmacyReportView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LowStockProductDto>>>> GetLowStock(CancellationToken ct) =>
        OkResponse(await stock.GetLowStockAsync(ct));

    [HttpGet("consumption-report")]
    [HasPermission(PermissionRegistry.PharmacyReportView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductConsumptionRowDto>>>> GetConsumptionReport(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct) =>
        OkResponse(await stock.GetConsumptionReportAsync(fromDate, toDate, ct));
}
