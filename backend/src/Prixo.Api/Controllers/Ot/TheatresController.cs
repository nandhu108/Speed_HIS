using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Ot;

namespace Prixo.Api.Controllers.Ot;

[Route("api/v1/ot/theatres")]
public class TheatresController(OtMasterService masters) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.OtTheatreView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TheatreDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await masters.GetTheatresAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.OtTheatreManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveTheatreRequest request, CancellationToken ct)
    {
        var id = await masters.CreateTheatreAsync(request, ct);
        return CreatedResponse($"/api/v1/ot/theatres/{id}", id, "Theatre created.");
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionRegistry.OtTheatreManage)]
    public async Task<ActionResult<ApiResponse>> Update(long id, SaveTheatreRequest request, CancellationToken ct)
    {
        await masters.UpdateTheatreAsync(id, request, ct);
        return OkResponse<object?>(null, "Theatre updated.");
    }

    [HttpGet("equipment")]
    [HasPermission(PermissionRegistry.OtTheatreView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentDto>>>> GetEquipment([FromQuery] long? theatreId, CancellationToken ct) =>
        OkResponse(await masters.GetEquipmentAsync(theatreId, ct));

    [HttpPost("equipment")]
    [HasPermission(PermissionRegistry.OtTheatreManage)]
    public async Task<ActionResult<ApiResponse<long>>> CreateEquipment(SaveEquipmentRequest request, CancellationToken ct)
    {
        var id = await masters.CreateEquipmentAsync(request, ct);
        return CreatedResponse($"/api/v1/ot/theatres/equipment/{id}", id, "Equipment created.");
    }

    [HttpPut("equipment/{id:long}")]
    [HasPermission(PermissionRegistry.OtTheatreManage)]
    public async Task<ActionResult<ApiResponse>> UpdateEquipment(long id, SaveEquipmentRequest request, CancellationToken ct)
    {
        await masters.UpdateEquipmentAsync(id, request, ct);
        return OkResponse<object?>(null, "Equipment updated.");
    }
}
