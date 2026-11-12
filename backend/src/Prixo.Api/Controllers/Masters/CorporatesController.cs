using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Corporates;

namespace Prixo.Api.Controllers.Masters;

public class CorporatesController(CorporateService corporates) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.CorporateView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CorporateDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) =>
        OkResponse(await corporates.GetAllAsync(search, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.CorporateManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveCorporateRequest request, CancellationToken ct)
    {
        var id = await corporates.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/corporates/{id}", id, "Corporate created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.CorporateManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveCorporateRequest request, CancellationToken ct)
    {
        await corporates.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Corporate updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.CorporateManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await corporates.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Corporate deleted.");
    }
}
