using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Sessions;

namespace Prixo.Api.Controllers;

public class SessionsController(SessionService sessions) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.SessionView)]
    public async Task<ActionResult<ApiResponse<PagedResult<SessionDto>>>> GetPaged(
        [FromQuery] PagedQuery query, [FromQuery] bool activeOnly = true,
        CancellationToken ct = default) =>
        OkResponse(await sessions.GetPagedAsync(query, activeOnly, ct));

    /// <summary>Force-logout. Explicit action endpoint, not a status PATCH.</summary>
    [HttpPost("{sessionId:guid}/revoke")]
    [HasPermission(PermissionRegistry.SessionRevoke)]
    public async Task<ActionResult<ApiResponse>> Revoke(Guid sessionId, CancellationToken ct)
    {
        await sessions.RevokeAsync(sessionId, ct);
        return OkResponse<object?>(null, "Session revoked.");
    }
}
