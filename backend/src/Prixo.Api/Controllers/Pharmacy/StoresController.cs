using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Pharmacy;

namespace Prixo.Api.Controllers.Pharmacy;

[Route("api/v1/pharmacy/stores")]
public class StoresController(StoreService stores) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.PharmacyStoreView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StoreDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await stores.GetAllAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.PharmacyStoreManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveStoreRequest request, CancellationToken ct)
    {
        var id = await stores.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/stores/{id}", id, "Store created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.PharmacyStoreManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveStoreRequest request, CancellationToken ct)
    {
        await stores.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Store updated.");
    }

    [HttpGet("racks")]
    [HasPermission(PermissionRegistry.PharmacyStoreView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RackDto>>>> GetRacks([FromQuery] long? storeId, CancellationToken ct) =>
        OkResponse(await stores.GetRacksAsync(storeId, ct));

    [HttpPost("racks")]
    [HasPermission(PermissionRegistry.PharmacyStoreManage)]
    public async Task<ActionResult<ApiResponse<long>>> CreateRack(SaveRackRequest request, CancellationToken ct)
    {
        var id = await stores.CreateRackAsync(request, ct);
        return CreatedResponse($"/api/v1/pharmacy/stores/racks/{id}", id, "Rack created.");
    }
}
