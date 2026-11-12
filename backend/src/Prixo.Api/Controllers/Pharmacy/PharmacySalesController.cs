using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/sales")]
public class PharmacySalesController(PharmacySaleService sales) : ApiControllerBase
{
    [HttpGet("batch-availability")]
    [HasPermission(PermissionRegistry.PharmacySaleCreate)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BatchAvailabilityDto>>>> GetBatchAvailability(
        [FromQuery] long drugId, [FromQuery] long storeId, CancellationToken ct) =>
        OkResponse(await sales.GetBatchAvailabilityAsync(drugId, storeId, ct));

    [HttpGet]
    [HasPermission(PermissionRegistry.PharmacySaleView)]
    public async Task<ActionResult<ApiResponse<PagedResult<PharmacySaleListItemDto>>>> GetPaged(
        [FromQuery] PharmacySaleQuery query, CancellationToken ct) =>
        OkResponse(await sales.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.PharmacySaleView)]
    public async Task<ActionResult<ApiResponse<PharmacySaleDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await sales.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.PharmacySaleCreate)]
    public async Task<ActionResult<ApiResponse<PharmacySaleDetailDto>>> Create(CreateSaleRequest request, CancellationToken ct)
    {
        var result = await sales.CreateSaleAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/sales/{result.Id}", result, $"Sale {result.SaleNumber} completed.");
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionRegistry.PharmacySaleCancel)]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, CancelSaleRequest request, CancellationToken ct)
    {
        await sales.CancelSaleAsync(id, request, ct);
        return OkResponse<object?>(null, "Sale cancelled.");
    }

    [HttpPut("{id:long}/pay-type")]
    [HasPermission(PermissionRegistry.PharmacySaleModify)]
    public async Task<ActionResult<ApiResponse>> ModifyPayType(long id, ModifyPayTypeRequest request, CancellationToken ct)
    {
        await sales.ModifyPayTypeAsync(id, request, ct);
        return OkResponse<object?>(null, "Payment mode updated.");
    }

    [HttpGet("{id:long}/invoice")]
    [HasPermission(PermissionRegistry.PharmacySaleView)]
    public async Task<IActionResult> GetInvoice(long id, CancellationToken ct)
    {
        var pdf = await sales.GenerateInvoiceAsync(id, ct);
        return File(pdf, "application/pdf", $"pharmacy-invoice-{id}.pdf");
    }
}
