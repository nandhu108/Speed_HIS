using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Lab;

namespace Prixo.Api.Controllers.Lab;

[Route("api/v1/lab/tests")]
public class LabTestsController(LabTestService tests) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.LabTestView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LabTestListItemDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await tests.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.LabTestView)]
    public async Task<ActionResult<ApiResponse<LabTestDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await tests.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.LabTestManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveLabTestRequest request, CancellationToken ct)
    {
        var id = await tests.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/lab/tests/{id}", id, "Lab test created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.LabTestManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveLabTestRequest request, CancellationToken ct)
    {
        await tests.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Lab test updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.LabTestManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await tests.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Lab test deleted.");
    }
}
