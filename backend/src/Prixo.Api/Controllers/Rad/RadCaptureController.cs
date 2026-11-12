using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Rad;

namespace Prixo.Api.Controllers.Rad;

[Route("api/v1/rad/capture")]
public class RadCaptureController(RadOrderService orders) : ApiControllerBase
{
    [HttpGet("worklist")]
    [HasPermission(PermissionRegistry.RadCaptureManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CaptureWorklistItemDto>>>> GetWorklist(
        [FromQuery] long? modalityId, CancellationToken ct) =>
        OkResponse(await orders.GetCaptureWorklistAsync(modalityId, ct));

    [HttpPost("order-items/{itemId:long}")]
    [HasPermission(PermissionRegistry.RadCaptureManage)]
    public async Task<ActionResult<ApiResponse>> MarkCaptured(long itemId, CancellationToken ct)
    {
        await orders.MarkImageCapturedAsync(itemId, ct);
        return OkResponse<object?>(null, "Image capture recorded.");
    }
}
