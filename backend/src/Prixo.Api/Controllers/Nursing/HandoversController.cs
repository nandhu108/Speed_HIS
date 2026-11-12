using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Nursing;

namespace Prixo.Api.Controllers.Nursing;

[Route("api/v1/nursing/handovers")]
public class HandoversController(HandoverService handovers) : ApiControllerBase
{
    [HttpGet("ward-patients")]
    [HasPermission(PermissionRegistry.HandoverView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WardPatientDto>>>> GetWardPatients([FromQuery] long wardId, CancellationToken ct) =>
        OkResponse(await handovers.GetWardPatientsAsync(wardId, ct));

    [HttpGet]
    [HasPermission(PermissionRegistry.HandoverView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShiftHandoverListItemDto>>>> Get(
        [FromQuery] long? wardId, [FromQuery] DateOnly? date, CancellationToken ct) =>
        OkResponse(await handovers.GetByWardAsync(wardId, date, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.HandoverView)]
    public async Task<ActionResult<ApiResponse<ShiftHandoverDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await handovers.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.HandoverManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(CreateShiftHandoverRequest request, CancellationToken ct)
    {
        var id = await handovers.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/nursing/handovers/{id}", id, "Shift handover recorded.");
    }
}
