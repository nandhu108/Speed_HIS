using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Masters.Services;

namespace Prixo.Api.Controllers.Masters;

public class ServicesController(ServiceCatalogService services) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.ServiceView)]
    public async Task<ActionResult<ApiResponse<PagedResult<ServiceDto>>>> GetPaged(
        [FromQuery] PagedQuery query, [FromQuery] long? categoryId, CancellationToken ct) =>
        OkResponse(await services.GetPagedAsync(query, categoryId, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.ServiceManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveServiceRequest request, CancellationToken ct)
    {
        var id = await services.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/services/{id}", id, "Service created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.ServiceManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveServiceRequest request, CancellationToken ct)
    {
        await services.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Service updated.");
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionRegistry.ServiceManage)]
    public async Task<ActionResult<ApiResponse>> Delete(long id, CancellationToken ct)
    {
        await services.DeleteAsync(id, ct);
        return OkResponse<object?>(null, "Service deleted.");
    }
}
