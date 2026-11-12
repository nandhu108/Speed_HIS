using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Billing;

namespace Prixo.Api.Controllers.Billing;

[Route("api/v1/billing/day-close")]
public class DayCloseController(DayCloseService dayClose) : ApiControllerBase
{
    [HttpGet("collection")]
    [HasPermission(PermissionRegistry.DayCloseView)]
    public async Task<ActionResult<ApiResponse<DailyCollectionDto>>> GetCollection(
        [FromQuery] DateOnly date, CancellationToken ct) =>
        OkResponse(await dayClose.GetDailyCollectionAsync(date, ct));

    [HttpGet]
    [HasPermission(PermissionRegistry.DayCloseView)]
    public async Task<ActionResult<ApiResponse<DayCloseDto?>>> GetByDate(
        [FromQuery] DateOnly date, CancellationToken ct) =>
        OkResponse(await dayClose.GetByDateAsync(date, ct));

    [HttpPost("close")]
    [HasPermission(PermissionRegistry.DayCloseClose)]
    public async Task<ActionResult<ApiResponse<DayCloseDto>>> Close(CloseDayRequest request, CancellationToken ct)
    {
        var result = await dayClose.CloseDayAsync(request, ct);
        return OkResponse(result, $"Day closed for {request.Date:yyyy-MM-dd}.");
    }
}
