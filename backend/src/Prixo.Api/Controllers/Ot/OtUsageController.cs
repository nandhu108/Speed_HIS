using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Ot;

namespace Prixo.Api.Controllers.Ot;

[Route("api/v1/ot/bookings/{bookingId:long}/usage")]
public class OtUsageController(OtUsageService usage) : ApiControllerBase
{
    [HttpGet("implants")]
    [HasPermission(PermissionRegistry.OtBookingView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ImplantUsageDto>>>> GetImplants(long bookingId, CancellationToken ct) =>
        OkResponse(await usage.GetImplantUsageAsync(bookingId, ct));

    [HttpPost("implants")]
    [HasPermission(PermissionRegistry.OtUsageRecord)]
    public async Task<ActionResult<ApiResponse<long>>> RecordImplant(long bookingId, RecordImplantUsageRequest request, CancellationToken ct)
    {
        var id = await usage.RecordImplantUsageAsync(bookingId, request, ct);
        return CreatedResponse($"/api/v1/ot/bookings/{bookingId}/usage/implants/{id}", id, "Implant usage recorded.");
    }

    [HttpGet("consumables")]
    [HasPermission(PermissionRegistry.OtBookingView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConsumableUsageDto>>>> GetConsumables(long bookingId, CancellationToken ct) =>
        OkResponse(await usage.GetConsumableUsageAsync(bookingId, ct));

    [HttpPost("consumables")]
    [HasPermission(PermissionRegistry.OtUsageRecord)]
    public async Task<ActionResult<ApiResponse<long>>> RecordConsumable(long bookingId, RecordConsumableUsageRequest request, CancellationToken ct)
    {
        var id = await usage.RecordConsumableUsageAsync(bookingId, request, ct);
        return CreatedResponse($"/api/v1/ot/bookings/{bookingId}/usage/consumables/{id}", id, "Consumable usage recorded.");
    }
}
