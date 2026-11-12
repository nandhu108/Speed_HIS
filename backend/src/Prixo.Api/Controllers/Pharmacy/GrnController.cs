using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/grn")]
public class GrnController(PurchaseService purchase) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.GrnView)]
    public async Task<ActionResult<ApiResponse<PagedResult<GrnListItemDto>>>> GetPaged(
        [FromQuery] GrnQuery query, CancellationToken ct) =>
        OkResponse(await purchase.GetGrnsAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.GrnView)]
    public async Task<ActionResult<ApiResponse<GrnDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await purchase.GetGrnByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.GrnCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateGrnRequest request, CancellationToken ct)
    {
        var id = await purchase.CreateGrnAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/grn/{id}", id, "GRN created.");
    }

    [HttpPost("{id:long}/post")]
    [HasPermission(PermissionRegistry.GrnCreate)]
    public async Task<ActionResult<ApiResponse<GrnDetailDto>>> Post(long id, CancellationToken ct) =>
        OkResponse(await purchase.PostGrnAsync(id, ct), "GRN posted — stock updated.");
}
