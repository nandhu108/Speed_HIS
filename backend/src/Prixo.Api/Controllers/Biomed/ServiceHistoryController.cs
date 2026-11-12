using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Biomed;

namespace Prixo.Api.Controllers.Biomed;

[Route("api/v1/biomed/equipment/{equipmentId:long}/service-history")]
public class ServiceHistoryController(ServiceHistoryService history) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.BiomedEquipmentView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceHistoryDto>>>> Get(long equipmentId, CancellationToken ct) =>
        OkResponse(await history.GetByEquipmentAsync(equipmentId, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.BiomedEquipmentManage)]
    public async Task<ActionResult<ApiResponse<long>>> Log(long equipmentId, LogServiceHistoryRequest request, CancellationToken ct)
    {
        if (request.EquipmentId != equipmentId)
        {
            request = request with { EquipmentId = equipmentId };
        }
        var id = await history.LogAsync(request, ct);
        return CreatedResponse($"/api/v1/biomed/equipment/{equipmentId}/service-history/{id}", id, "Service history logged.");
    }
}
