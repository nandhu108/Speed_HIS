using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Menus;
using Prixo.Application.Features.Admin.Roles;

namespace Prixo.Api.Controllers;

public class RolesController(RoleService roles, MenuService menus) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.RoleView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await roles.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.RoleCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(
        SaveRoleRequest request, CancellationToken ct)
    {
        var id = await roles.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/roles/{id}", id, "Role created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.RoleUpdate)]
    public async Task<ActionResult<ApiResponse>> Update(
        long id, SaveRoleRequest request, CancellationToken ct)
    {
        await roles.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Role updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.RoleDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await roles.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Role deleted.");
    }

    [HttpGet("{id:long}/permissions")]
    [HasPermission(PermissionRegistry.RoleView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> GetPermissions(
        long id, CancellationToken ct) =>
        OkResponse(await roles.GetRolePermissionsAsync(id, ct));

    [HttpPut("{id:long}/permissions")]
    [HasPermission(PermissionRegistry.RoleAssignPermissions)]
    public async Task<ActionResult<ApiResponse>> SetPermissions(
        long id, SetRolePermissionsRequest request, CancellationToken ct)
    {
        await roles.SetPermissionsAsync(id, request, ct);
        return OkResponse<object?>(null, "Role permissions updated.");
    }

    /// <summary>Resets a system role's permissions back to its seeded default (DefaultRoleDefinitions) — undoes hand-tuning drift.</summary>
    [HttpPost("{id:long}/restore-defaults")]
    [HasPermission(PermissionRegistry.RoleAssignPermissions)]
    public async Task<ActionResult<ApiResponse>> RestoreDefaults(long id, CancellationToken ct)
    {
        await roles.RestoreDefaultPermissionsAsync(id, ct);
        return OkResponse<object?>(null, "Role permissions restored to default.");
    }

    /// <summary>This role's menu tree with its own position overrides applied — powers the Permissions modal's "By menu" tab.</summary>
    [HttpGet("{id:long}/menu-tree")]
    [HasPermission(PermissionRegistry.RoleView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MenuNodeDto>>>> GetMenuTree(long id, CancellationToken ct) =>
        OkResponse(await menus.GetTreeForRoleAsync(id, ct));

    [HttpPut("{id:long}/menu-layout/{menuId:long}")]
    [HasPermission(PermissionRegistry.RoleAssignPermissions)]
    public async Task<ActionResult<ApiResponse>> SetMenuPosition(
        long id, long menuId, SetMenuPositionRequest request, CancellationToken ct)
    {
        await menus.SetRoleMenuPositionAsync(id, menuId, request, ct);
        return OkResponse<object?>(null, "Menu position updated.");
    }

    [HttpDelete("{id:long}/menu-layout/{menuId:long}")]
    [HasPermission(PermissionRegistry.RoleAssignPermissions)]
    public async Task<ActionResult<ApiResponse>> ResetMenuPosition(long id, long menuId, CancellationToken ct)
    {
        await menus.ResetRoleMenuPositionAsync(id, menuId, ct);
        return OkResponse<object?>(null, "Menu position reset.");
    }
}
