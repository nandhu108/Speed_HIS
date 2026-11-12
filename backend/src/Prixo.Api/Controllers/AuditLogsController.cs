using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Logs;

namespace Prixo.Api.Controllers;

[Route("api/v1/audit-logs")]
public class AuditLogsController(LogQueryService logs) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.AuditView)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> GetPaged(
        [FromQuery] AuditLogQuery query, CancellationToken ct) =>
        OkResponse(await logs.GetAuditLogsAsync(query, ct));
}
