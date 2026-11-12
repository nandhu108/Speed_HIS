using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Nursing;

namespace Prixo.Api.Controllers.Nursing;

[Route("api/v1/nursing/mar")]
public class MarController(MarService mar) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.MarView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MarScheduleDto>>>> Get(
        [FromQuery] long admissionId, [FromQuery] DateOnly date, CancellationToken ct) =>
        OkResponse(await mar.GetAsync(admissionId, date, ct));

    [HttpPost("{id:long}/administer")]
    [HasPermission(PermissionRegistry.MarAdminister)]
    public async Task<ActionResult<ApiResponse>> Action(long id, MarActionRequest request, CancellationToken ct)
    {
        await mar.ActionAsync(id, request, ct);
        return OkResponse<object?>(null, "Dose recorded.");
    }
}
