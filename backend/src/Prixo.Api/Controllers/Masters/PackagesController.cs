using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Packages;

namespace Prixo.Api.Controllers.Masters;

public class PackagesController(PackageService packages) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.PackageView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PackageListItemDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await packages.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.PackageView)]
    public async Task<ActionResult<ApiResponse<PackageDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await packages.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.PackageManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SavePackageRequest request, CancellationToken ct)
    {
        var id = await packages.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/packages/{id}", id, "Package created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.PackageManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SavePackageRequest request, CancellationToken ct)
    {
        await packages.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Package updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.PackageManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await packages.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Package deleted.");
    }
}
