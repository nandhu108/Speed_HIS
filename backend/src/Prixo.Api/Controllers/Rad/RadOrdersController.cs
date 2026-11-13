using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Rad;

namespace Prixo.Api.Controllers.Rad;

[Route("api/v1/rad/orders")]
public class RadOrdersController(RadOrderService orders) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.RadOrderView)]
    public async Task<ActionResult<ApiResponse<PagedResult<RadOrderListItemDto>>>> GetPaged(
        [FromQuery] RadOrderQuery query, CancellationToken ct) =>
        OkResponse(await orders.GetPagedAsync(query, ct));

    [HttpGet("tat-report")]
    [HasPermission(PermissionRegistry.RadReportView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RadTatReportRowDto>>>> GetTatReport(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct) =>
        OkResponse(await orders.GetTatReportAsync(fromDate, toDate, ct));

    [HttpGet("unbilled-report")]
    [HasPermission(PermissionRegistry.RadReportView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UnbilledRadOrderRowDto>>>> GetUnbilledOrdersReport(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        // Default window: the last 30 days up to today (a bare call used to return nothing).
        var to = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5));
        return OkResponse(await orders.GetUnbilledOrdersReportAsync(fromDate ?? to.AddDays(-30), to, ct));
    }

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.RadOrderView)]
    public async Task<ActionResult<ApiResponse<RadOrderDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await orders.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.RadOrderCreate)]
    public async Task<ActionResult<ApiResponse<RadOrderDetailDto>>> Create(CreateRadOrderRequest request, CancellationToken ct)
    {
        var result = await orders.CreateOrderAsync(request, ct);
        return CreatedResponse($"/api/v1/rad/orders/{result.Id}", result, $"Radiology order {result.OrderNumber} created.");
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionRegistry.RadOrderCancel)]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, CancelRadOrderRequest request, CancellationToken ct)
    {
        await orders.CancelOrderAsync(id, request, ct);
        return OkResponse<object?>(null, "Radiology order cancelled.");
    }

    [HttpPost("{id:long}/items/{itemId:long}/cancel")]
    [HasPermission(PermissionRegistry.RadOrderCancel)]
    public async Task<ActionResult<ApiResponse>> CancelItem(long id, long itemId, CancellationToken ct)
    {
        await orders.CancelItemAsync(id, itemId, ct);
        return OkResponse<object?>(null, "Test cancelled.");
    }
}
