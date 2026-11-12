using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Settings;

namespace Prixo.Api.Controllers;

public class SettingsController(SettingService settings) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.SettingView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SettingDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await settings.GetAllAsync(ct));

    [HttpPut("{key}")]
    [HasPermission(PermissionRegistry.SettingUpdate)]
    public async Task<ActionResult<ApiResponse>> Update(
        string key, UpdateSettingRequest request, CancellationToken ct)
    {
        await settings.UpdateAsync(key, request, ct);
        return OkResponse<object?>(null, "Setting updated.");
    }
}
