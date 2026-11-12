using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Drugs;

namespace Prixo.Api.Controllers.Masters;

public class DrugsController(DrugService drugs) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.DrugView)]
    public async Task<ActionResult<ApiResponse<PagedResult<DrugDto>>>> GetPaged(
        [FromQuery] PagedQuery query, CancellationToken ct) =>
        OkResponse(await drugs.GetPagedAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.DrugManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveDrugRequest request, CancellationToken ct)
    {
        var id = await drugs.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/drugs/{id}", id, "Drug created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.DrugManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveDrugRequest request, CancellationToken ct)
    {
        await drugs.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Drug updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.DrugManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await drugs.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Drug deleted.");
    }
}
