using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Billing;

namespace Prixo.Api.Controllers.Masters;

public class TaxesController(TaxService taxes) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.TaxView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TaxDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await taxes.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.TaxManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveTaxRequest request, CancellationToken ct)
    {
        var id = await taxes.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/taxes/{id}", id, "Tax slab created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.TaxManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveTaxRequest request, CancellationToken ct)
    {
        await taxes.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Tax slab updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.TaxManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await taxes.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Tax slab deleted.");
    }
}
