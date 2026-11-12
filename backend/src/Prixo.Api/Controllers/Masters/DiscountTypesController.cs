using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Billing;

namespace Prixo.Api.Controllers.Masters;

public class DiscountTypesController(DiscountTypeService discountTypes) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.DiscountTypeView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DiscountTypeDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await discountTypes.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.DiscountTypeManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveDiscountTypeRequest request, CancellationToken ct)
    {
        var id = await discountTypes.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/discounttypes/{id}", id, "Discount type created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.DiscountTypeManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveDiscountTypeRequest request, CancellationToken ct)
    {
        await discountTypes.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Discount type updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.DiscountTypeManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await discountTypes.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Discount type deleted.");
    }
}
