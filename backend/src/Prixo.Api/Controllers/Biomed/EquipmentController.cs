using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Biomed;

namespace Prixo.Api.Controllers.Biomed;

[Route("api/v1/biomed/equipment")]
public class EquipmentController(EquipmentService equipment) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.BiomedEquipmentView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentListItemDto>>>> GetAll([FromQuery] EquipmentQuery query, CancellationToken ct) =>
        OkResponse(await equipment.GetListAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.BiomedEquipmentView)]
    public async Task<ActionResult<ApiResponse<EquipmentDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await equipment.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.BiomedEquipmentManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveEquipmentRequest request, CancellationToken ct)
    {
        var id = await equipment.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/biomed/equipment/{id}", id, "Equipment created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.BiomedEquipmentManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveEquipmentRequest request, CancellationToken ct)
    {
        await equipment.UpdateAsync(id, request, ct);
        return OkResponse<object?>(null, "Equipment updated.");
    }

    [HttpGet("{id:long}/locations")]
    [HasPermission(PermissionRegistry.BiomedEquipmentView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentLocationLogDto>>>> GetLocationHistory(long id, CancellationToken ct) =>
        OkResponse(await equipment.GetLocationHistoryAsync(id, ct));

    [HttpPost("{id:long}/locations")]
    [HasPermission(PermissionRegistry.BiomedEquipmentManage)]
    public async Task<ActionResult<ApiResponse<long>>> RecordMovement(long id, RecordEquipmentMovementRequest request, CancellationToken ct) =>
        OkResponse(await equipment.RecordMovementAsync(id, request, ct), "Movement recorded.");
}
