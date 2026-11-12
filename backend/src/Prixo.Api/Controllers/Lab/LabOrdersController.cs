using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Lab;

namespace Prixo.Api.Controllers.Lab;

[Route("api/v1/lab/orders")]
public class LabOrdersController(LabOrderService orders) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.LabOrderView)]
    public async Task<ActionResult<ApiResponse<PagedResult<LabOrderListItemDto>>>> GetPaged(
        [FromQuery] LabOrderQuery query, CancellationToken ct) =>
        OkResponse(await orders.GetPagedAsync(query, ct));

    [HttpGet("tat-report")]
    [HasPermission(PermissionRegistry.LabReportView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TatReportRowDto>>>> GetTatReport(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct) =>
        OkResponse(await orders.GetTatReportAsync(fromDate, toDate, ct));

    [HttpGet("unbilled-report")]
    [HasPermission(PermissionRegistry.LabReportView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UnbilledLabOrderRowDto>>>> GetUnbilledOrdersReport(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct) =>
        OkResponse(await orders.GetUnbilledOrdersReportAsync(fromDate, toDate, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.LabOrderView)]
    public async Task<ActionResult<ApiResponse<LabOrderDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await orders.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.LabOrderCreate)]
    public async Task<ActionResult<ApiResponse<LabOrderDetailDto>>> Create(CreateLabOrderRequest request, CancellationToken ct)
    {
        var result = await orders.CreateOrderAsync(request, ct);
        return CreatedResponse($"/api/v1/lab/orders/{result.Id}", result, $"Lab order {result.OrderNumber} created.");
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionRegistry.LabOrderCancel)]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, CancelLabOrderRequest request, CancellationToken ct)
    {
        await orders.CancelOrderAsync(id, request, ct);
        return OkResponse<object?>(null, "Lab order cancelled.");
    }

    [HttpPost("{id:long}/items/{itemId:long}/cancel")]
    [HasPermission(PermissionRegistry.LabOrderCancel)]
    public async Task<ActionResult<ApiResponse>> CancelItem(long id, long itemId, CancellationToken ct)
    {
        await orders.CancelItemAsync(id, itemId, ct);
        return OkResponse<object?>(null, "Test cancelled.");
    }

    [HttpGet("{id:long}/report")]
    [HasPermission(PermissionRegistry.LabReportView)]
    public async Task<IActionResult> GetReport(long id, CancellationToken ct)
    {
        var pdf = await orders.GenerateReportAsync(id, ct);
        return File(pdf, "application/pdf", $"lab-report-{id}.pdf");
    }
}
