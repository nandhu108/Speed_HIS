using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.MhcPackages;

namespace Prixo.Api.Controllers.Masters;

/// <summary>MHC checkup package master — bespoke (not the flat SimpleMasterControllerBase) since it carries a Price field.</summary>
[Route("api/v1/mhc-packages")]
public class MhcPackagesController(MhcPackageService service) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.MhcPackageView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MhcPackageDto>>>> GetAll(
        [FromQuery] string? search, CancellationToken ct) =>
        OkResponse(await service.GetAllAsync(search, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.MhcPackageManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveMhcPackageRequest request, CancellationToken ct)
    {
        var id = await service.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/mhc-packages/{id}", id, "MHC package created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.MhcPackageManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveMhcPackageRequest request, CancellationToken ct)
    {
        await service.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "MHC package updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.MhcPackageManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "MHC package deleted.");
    }
}
