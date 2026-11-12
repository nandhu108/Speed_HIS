using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Logs;

namespace Prixo.Api.Controllers;

[Route("api/v1/activity-logs")]
public class ActivityLogsController(LogQueryService logs) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.ActivityView)]
    public async Task<ActionResult<ApiResponse<PagedResult<ActivityLogDto>>>> GetPaged(
        [FromQuery] ActivityLogQuery query, CancellationToken ct) =>
        OkResponse(await logs.GetActivityLogsAsync(query, ct));
}
