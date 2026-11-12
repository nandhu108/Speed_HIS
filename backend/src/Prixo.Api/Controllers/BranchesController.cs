using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Branches;

namespace Prixo.Api.Controllers;

public class BranchesController(BranchService branches) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.BranchView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BranchDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await branches.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.BranchView)]
    public async Task<ActionResult<ApiResponse<BranchDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await branches.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.BranchManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(
        SaveBranchRequest request, CancellationToken ct)
    {
        var id = await branches.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/branches/{id}", id, "Branch created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.BranchManage)]
    public async Task<ActionResult<ApiResponse>> Update(
        long id, SaveBranchRequest request, CancellationToken ct)
    {
        await branches.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Branch updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.BranchManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await branches.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Branch deleted.");
    }
}
