using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Items;

namespace Prixo.Api.Controllers.Masters;

public class ItemsController(ItemService items) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.ItemView)]
    public async Task<ActionResult<ApiResponse<PagedResult<ItemDto>>>> GetPaged(
        [FromQuery] PagedQuery query, CancellationToken ct) =>
        OkResponse(await items.GetPagedAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.ItemManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveItemRequest request, CancellationToken ct)
    {
        var id = await items.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/items/{id}", id, "Item created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.ItemManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveItemRequest request, CancellationToken ct)
    {
        await items.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Item updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.ItemManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await items.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Item deleted.");
    }
}
