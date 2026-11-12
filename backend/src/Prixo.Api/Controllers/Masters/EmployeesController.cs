using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Employees;

namespace Prixo.Api.Controllers.Masters;

public class EmployeesController(EmployeeService employees) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.EmployeeView)]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeDto>>>> GetPaged(
        [FromQuery] PagedQuery query, CancellationToken ct) =>
        OkResponse(await employees.GetPagedAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.EmployeeManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveEmployeeRequest request, CancellationToken ct)
    {
        var id = await employees.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/employees/{id}", id, "Employee created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.EmployeeManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveEmployeeRequest request, CancellationToken ct)
    {
        await employees.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Employee updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.EmployeeManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await employees.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Employee deleted.");
    }
}
