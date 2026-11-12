using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Lab;

namespace Prixo.Api.Controllers.Lab;

[Route("api/v1/lab/profiles")]
public class TestProfilesController(TestProfileService profiles) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.LabProfileView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TestProfileListItemDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await profiles.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.LabProfileView)]
    public async Task<ActionResult<ApiResponse<TestProfileDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await profiles.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.LabProfileManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveTestProfileRequest request, CancellationToken ct)
    {
        var id = await profiles.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/lab/profiles/{id}", id, "Test profile created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.LabProfileManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveTestProfileRequest request, CancellationToken ct)
    {
        await profiles.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Test profile updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.LabProfileManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await profiles.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Test profile deleted.");
    }
}
