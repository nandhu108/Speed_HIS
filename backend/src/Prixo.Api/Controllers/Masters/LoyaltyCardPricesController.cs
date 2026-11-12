using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Loyalty;

namespace Prixo.Api.Controllers.Masters;

/// <summary>The "Loyalty Card Price Matrix" — branch price list per card type with an effective window (header+child, mirrors BillEstimateTemplatesController).</summary>
[Route("api/v1/loyalty-card-prices")]
public class LoyaltyCardPricesController(LoyaltyCardPriceService prices) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.LoyaltyCardPriceView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LoyaltyCardPriceListItemDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await prices.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.LoyaltyCardPriceView)]
    public async Task<ActionResult<ApiResponse<LoyaltyCardPriceDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await prices.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.LoyaltyCardPriceManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveLoyaltyCardPriceRequest request, CancellationToken ct)
    {
        var id = await prices.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/loyalty-card-prices/{id}", id, "Loyalty card price created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.LoyaltyCardPriceManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveLoyaltyCardPriceRequest request, CancellationToken ct)
    {
        await prices.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Loyalty card price updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.LoyaltyCardPriceManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await prices.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Loyalty card price deleted.");
    }
}
