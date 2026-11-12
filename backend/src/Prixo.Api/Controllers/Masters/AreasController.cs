using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Areas;

namespace Prixo.Api.Controllers.Masters;

/// <summary>Area-within-City lookup — bespoke (not the flat SimpleMasterControllerBase) since it carries a CityId FK.</summary>
public class AreasController(AreaService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.AreaView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AreaDto>>>> GetAll(
        [FromQuery] long? cityId, CancellationToken ct) =>
        OkResponse(await service.GetAllAsync(cityId, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.AreaManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveAreaRequest request, CancellationToken ct)
    {
        var id = await service.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/areas/{id}", id, "Area created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.AreaManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveAreaRequest request, CancellationToken ct)
    {
        await service.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Area updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.AreaManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Area deleted.");
    }
}
