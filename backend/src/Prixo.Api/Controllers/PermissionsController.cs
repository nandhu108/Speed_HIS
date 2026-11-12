using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Roles;

namespace Prixo.Api.Controllers;

public class PermissionsController(RoleService roles) : ApiControllerBase
{
    /// <summary>The full permission catalogue (for the role permission matrix).</summary>
    [HttpGet]
    [HasPermission(PermissionRegistry.PermissionView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await roles.GetPermissionCatalogueAsync(ct));
}
