using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Roles;

namespace Prixo.Api.Controllers;

/// <summary>Named, reusable role combinations for fast user creation — gated by the same permission as creating users.</summary>
[Route("api/v1/role-presets")]
public class RolePresetsController(RolePresetService presets) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.UserCreate)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RolePresetDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await presets.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.UserCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveRolePresetRequest request, CancellationToken ct)
    {
        var id = await presets.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/role-presets/{id}", id, "Preset saved.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.UserCreate)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await presets.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Preset deleted.");
    }
}
