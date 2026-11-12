using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Rad;

namespace Prixo.Api.Controllers.Rad;

[Route("api/v1/rad/scheduling")]
public class RadSchedulingController(RadOrderService orders) : ApiControllerBase
{
    [HttpGet("worklist")]
    [HasPermission(PermissionRegistry.RadScheduleManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SchedulingWorklistItemDto>>>> GetWorklist(
        [FromQuery] long? modalityId, CancellationToken ct) =>
        OkResponse(await orders.GetSchedulingWorklistAsync(modalityId, ct));

    [HttpPost("order-items/{itemId:long}")]
    [HasPermission(PermissionRegistry.RadScheduleManage)]
    public async Task<ActionResult<ApiResponse>> Schedule(long itemId, ScheduleItemRequest request, CancellationToken ct)
    {
        await orders.ScheduleItemAsync(itemId, request, ct);
        return OkResponse<object?>(null, "Test scheduled.");
    }
}
