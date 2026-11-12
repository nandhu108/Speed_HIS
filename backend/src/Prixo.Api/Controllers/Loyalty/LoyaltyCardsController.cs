using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Loyalty;

namespace Prixo.Api.Controllers.Loyalty;

[Route("api/v1/loyalty-cards")]
public class LoyaltyCardsController(LoyaltyCardService cards) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.LoyaltyCardView)]
    public async Task<ActionResult<ApiResponse<PagedResult<LoyaltyCardListItemDto>>>> GetPaged(
        [FromQuery] LoyaltyCardQuery query, CancellationToken ct) =>
        OkResponse(await cards.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.LoyaltyCardView)]
    public async Task<ActionResult<ApiResponse<LoyaltyCardDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await cards.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.LoyaltyCardCreate)]
    public async Task<ActionResult<ApiResponse<LoyaltyCardDetailDto>>> Register(
        RegisterLoyaltyCardRequest request, CancellationToken ct)
    {
        var result = await cards.RegisterAsync(request, ct);
        return CreatedResponse($"/api/v1/loyalty-cards/{result.Id}", result, $"Loyalty card {result.CardNumber} registered.");
    }

    [HttpPost("{id:long}/renew")]
    [HasPermission(PermissionRegistry.LoyaltyCardManage)]
    public async Task<ActionResult<ApiResponse<LoyaltyCardDetailDto>>> Renew(
        long id, RenewLoyaltyCardRequest request, CancellationToken ct) =>
        OkResponse(await cards.RenewOrUpgradeAsync(id, request, ct));

    [HttpPost("{id:long}/members")]
    [HasPermission(PermissionRegistry.LoyaltyCardManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddMember(long id, AddDependentRequest request, CancellationToken ct)
    {
        var memberId = await cards.AddDependentAsync(id, request, ct);
        return CreatedResponse($"/api/v1/loyalty-cards/{id}/members/{memberId}", memberId, "Dependent added.");
    }

    [HttpDelete("{id:long}/members/{memberId:long}")]
    [HasPermission(PermissionRegistry.LoyaltyCardManage)]
    public async Task<ActionResult<ApiResponse>> RemoveMember(long id, long memberId, CancellationToken ct)
    {
        await cards.RemoveDependentAsync(id, memberId, ct);
        return OkResponse<object?>(null, "Dependent removed.");
    }

    [HttpPost("{id:long}/duplicate")]
    [HasPermission(PermissionRegistry.LoyaltyCardManage)]
    public async Task<ActionResult<ApiResponse<LoyaltyCardDetailDto>>> Duplicate(long id, CancellationToken ct) =>
        OkResponse(await cards.DuplicateIssueAsync(id, ct));

    [HttpPost("{id:long}/deactivate")]
    [HasPermission(PermissionRegistry.LoyaltyCardDeactivate)]
    public async Task<ActionResult<ApiResponse>> Deactivate(long id, DeactivateLoyaltyCardRequest request, CancellationToken ct)
    {
        await cards.DeactivateAsync(id, request, ct);
        return OkResponse<object?>(null, "Loyalty card deactivated.");
    }

    [HttpGet("{id:long}/card")]
    [HasPermission(PermissionRegistry.LoyaltyCardView)]
    public async Task<IActionResult> GetCard(long id, CancellationToken ct)
    {
        var pdf = await cards.GenerateCardPdfAsync(id, ct);
        return File(pdf, "application/pdf", $"loyalty-card-{id}.pdf");
    }
}
