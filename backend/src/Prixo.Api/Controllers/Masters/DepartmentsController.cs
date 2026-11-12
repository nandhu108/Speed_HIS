using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Departments;
using Prixo.Domain.Enums;

namespace Prixo.Api.Controllers.Masters;

/// <summary>Department master — bespoke since it carries a Clinical / Non-Clinical type. Route
/// auto-derives to api/v1/departments via [controller], matching the pre-existing endpoint.
/// `type` filters the list (OP/IP registration pass type=1 to get clinical departments only).</summary>
public class DepartmentsController(DepartmentService departments) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.DepartmentView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentDto>>>> GetAll(
        [FromQuery] string? search, [FromQuery] DepartmentType? type, CancellationToken ct) =>
        OkResponse(await departments.GetAllAsync(search, type, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.DepartmentManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveDepartmentRequest request, CancellationToken ct)
    {
        var id = await departments.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/departments/{id}", id, "Department created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.DepartmentManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveDepartmentRequest request, CancellationToken ct)
    {
        await departments.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Department updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.DepartmentManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await departments.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Department deleted.");
    }
}
