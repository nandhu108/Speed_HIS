using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Menus;

namespace Prixo.Api.Controllers;

public class MenusController(MenuService menus) : ApiControllerBase
{
    /// <summary>The caller's sidebar. Any authenticated user; filtered by permissions.</summary>
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MenuNodeDto>>>> My(CancellationToken ct) =>
        OkResponse(await menus.GetMyMenuAsync(ct));

    [HttpGet]
    [HasPermission(PermissionRegistry.MenuView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MenuNodeDto>>>> GetTree(CancellationToken ct) =>
        OkResponse(await menus.GetTreeAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.MenuManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(
        SaveMenuRequest request, CancellationToken ct)
    {
        var id = await menus.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/menus/{id}", id, "Menu created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.MenuManage)]
    public async Task<ActionResult<ApiResponse>> Update(
        long id, SaveMenuRequest request, CancellationToken ct)
    {
        await menus.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Menu updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.MenuManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await menus.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Menu deleted.");
    }
}
