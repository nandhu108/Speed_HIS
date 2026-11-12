using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Rad;

namespace Prixo.Api.Controllers.Rad;

[Route("api/v1/rad/tests")]
public class RadiologyTestsController(RadiologyTestService tests) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.RadTestView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RadiologyTestListItemDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await tests.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.RadTestView)]
    public async Task<ActionResult<ApiResponse<RadiologyTestDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await tests.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.RadTestManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveRadiologyTestRequest request, CancellationToken ct)
    {
        var id = await tests.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/rad/tests/{id}", id, "Radiology test created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.RadTestManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveRadiologyTestRequest request, CancellationToken ct)
    {
        await tests.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Radiology test updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.RadTestManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await tests.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Radiology test deleted.");
    }
}
