using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Vendors;

namespace Prixo.Api.Controllers.Masters;

public class VendorsController(VendorService vendors) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.VendorView)]
    public async Task<ActionResult<ApiResponse<PagedResult<VendorDto>>>> GetPaged(
        [FromQuery] PagedQuery query, CancellationToken ct) =>
        OkResponse(await vendors.GetPagedAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.VendorManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveVendorRequest request, CancellationToken ct)
    {
        var id = await vendors.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/vendors/{id}", id, "Vendor created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.VendorManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveVendorRequest request, CancellationToken ct)
    {
        await vendors.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Vendor updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.VendorManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await vendors.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Vendor deleted.");
    }
}
