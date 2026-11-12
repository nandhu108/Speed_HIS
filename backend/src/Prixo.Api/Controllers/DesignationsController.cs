using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Admin.Designations;

namespace Prixo.Api.Controllers;

public class DesignationsController(DesignationService designations) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.DesignationView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DesignationDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await designations.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.DesignationManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(
        SaveDesignationRequest request, CancellationToken ct)
    {
        var id = await designations.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/designations/{id}", id, "Designation created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.DesignationManage)]
    public async Task<ActionResult<ApiResponse>> Update(
        long id, SaveDesignationRequest request, CancellationToken ct)
    {
        await designations.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Designation updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.DesignationManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await designations.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Designation deleted.");
    }
}
