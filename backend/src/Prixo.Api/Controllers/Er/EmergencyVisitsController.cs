using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Er;

namespace Prixo.Api.Controllers.Er;

[Route("api/v1/emergency/visits")]
public class EmergencyVisitsController(EmergencyVisitService visits) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.EmergencyVisitView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EmergencyVisitListItemDto>>>> GetQueue([FromQuery] EmergencyVisitQuery query, CancellationToken ct) =>
        OkResponse(await visits.GetQueueAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.EmergencyVisitView)]
    public async Task<ActionResult<ApiResponse<EmergencyVisitDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await visits.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.EmergencyVisitCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Register(RegisterEmergencyVisitRequest request, CancellationToken ct)
    {
        var id = await visits.RegisterAsync(request, ct);
        return CreatedResponse($"/api/v1/emergency/visits/{id}", id, "ER visit registered.");
    }

    [HttpPost("{id:long}/triage")]
    [HasPermission(PermissionRegistry.EmergencyTriage)]
    public async Task<ActionResult<ApiResponse<TriageDto>>> RecordTriage(long id, RecordTriageRequest request, CancellationToken ct) =>
        OkResponse(await visits.RecordTriageAsync(id, request, ct), "Triage recorded.");

    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionRegistry.EmergencyVisitManage)]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, CancelEmergencyVisitRequest request, CancellationToken ct)
    {
        await visits.CancelAsync(id, request, ct);
        return OkResponse<object?>(null, "ER visit cancelled.");
    }
}
