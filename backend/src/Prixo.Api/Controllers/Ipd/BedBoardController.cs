using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Ipd;

namespace Prixo.Api.Controllers.Ipd;

[Route("api/v1/ipd/bed-board")]
public class BedBoardController(BedBoardService bedBoard) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.BedView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BedBoardItemDto>>>> GetBoard(
        [FromQuery] long? wardId, CancellationToken ct) =>
        OkResponse(await bedBoard.GetBoardAsync(wardId, ct));

    [HttpGet("occupancy")]
    [HasPermission(PermissionRegistry.BedView)]
    public async Task<ActionResult<ApiResponse<BedOccupancyDashboardDto>>> GetOccupancy(CancellationToken ct) =>
        OkResponse(await bedBoard.GetOccupancyDashboardAsync(ct));

    [HttpPost("{bedId:long}/status")]
    [HasPermission(PermissionRegistry.BedManage)]
    public async Task<ActionResult<ApiResponse>> SetStatus(long bedId, SetBedStatusRequest request, CancellationToken ct)
    {
        await bedBoard.SetStatusAsync(bedId, request, ct);
        return OkResponse<object?>(null, "Bed status updated.");
    }
}
