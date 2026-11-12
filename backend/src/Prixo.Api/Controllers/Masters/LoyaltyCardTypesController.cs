using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Loyalty;

namespace Prixo.Api.Controllers.Masters;

/// <summary>Loyalty card product master — bespoke (not SimpleMasterControllerBase) since it carries MemberLimit + an optional category FK.</summary>
[Route("api/v1/loyalty-card-types")]
public class LoyaltyCardTypesController(LoyaltyCardTypeService types) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.LoyaltyCardTypeView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LoyaltyCardTypeDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await types.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.LoyaltyCardTypeManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveLoyaltyCardTypeRequest request, CancellationToken ct)
    {
        var id = await types.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/loyalty-card-types/{id}", id, "Loyalty card type created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.LoyaltyCardTypeManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveLoyaltyCardTypeRequest request, CancellationToken ct)
    {
        await types.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Loyalty card type updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.LoyaltyCardTypeManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await types.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Loyalty card type deleted.");
    }
}
