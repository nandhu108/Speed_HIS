using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Users;

namespace Prixo.Api.Controllers;

public class UsersController(UserService users) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.UserView)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserListItemDto>>>> GetPaged(
        [FromQuery] UserQuery query, CancellationToken ct) =>
        OkResponse(await users.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.UserView)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await users.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.UserCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(
        CreateUserRequest request, CancellationToken ct)
    {
        var id = await users.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/users/{id}", id, "User created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.UserUpdate)]
    public async Task<ActionResult<ApiResponse>> Update(
        long id, UpdateUserRequest request, CancellationToken ct)
    {
        await users.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "User updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.UserDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await users.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "User deleted.");
    }

    [HttpPost("{id:long}/activate")]
    [HasPermission(PermissionRegistry.UserUpdate)]
    public async Task<ActionResult<ApiResponse>> Activate(long id, CancellationToken ct)
    {
        await users.SetActiveAsync(id, true, ct);
        return OkResponse<object?>(null, "User activated.");
    }

    [HttpPost("{id:long}/deactivate")]
    [HasPermission(PermissionRegistry.UserUpdate)]
    public async Task<ActionResult<ApiResponse>> Deactivate(long id, CancellationToken ct)
    {
        await users.SetActiveAsync(id, false, ct);
        return OkResponse<object?>(null, "User deactivated.");
    }

    [HttpPost("{id:long}/reset-password")]
    [HasPermission(PermissionRegistry.UserResetPassword)]
    public async Task<ActionResult<ApiResponse>> ResetPassword(
        long id, ResetPasswordRequest request, CancellationToken ct)
    {
        await users.ResetPasswordAsync(id, request, ct);
        return OkResponse<object?>(null, "Password reset.");
    }

    [HttpGet("{id:long}/permission-overrides")]
    [HasPermission(PermissionRegistry.UserManagePermissions)]
    public async Task<ActionResult<ApiResponse<UserPermissionsDto>>> GetPermissionOverrides(
        long id, CancellationToken ct) =>
        OkResponse(await users.GetPermissionOverridesAsync(id, ct));

    [HttpPut("{id:long}/permission-overrides")]
    [HasPermission(PermissionRegistry.UserManagePermissions)]
    public async Task<ActionResult<ApiResponse>> SetPermissionOverrides(
        long id, SetUserPermissionOverridesRequest request, CancellationToken ct)
    {
        await users.SetPermissionOverridesAsync(id, request, ct);
        return OkResponse<object?>(null, "Permission overrides updated.");
    }
}
