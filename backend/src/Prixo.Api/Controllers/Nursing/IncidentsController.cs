using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Nursing;

namespace Prixo.Api.Controllers.Nursing;

[Route("api/v1/nursing/incidents")]
public class IncidentsController(IncidentService incidents) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.IncidentView)]
    public async Task<ActionResult<ApiResponse<PagedResult<IncidentDto>>>> GetPaged([FromQuery] IncidentQuery query, CancellationToken ct) =>
        OkResponse(await incidents.GetPagedAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.IncidentCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Report(ReportIncidentRequest request, CancellationToken ct)
    {
        var id = await incidents.ReportAsync(request, ct);
        return CreatedResponse($"/api/v1/nursing/incidents/{id}", id, "Incident reported.");
    }

    [HttpPost("{id:long}/review")]
    [HasPermission(PermissionRegistry.IncidentReview)]
    public async Task<ActionResult<ApiResponse>> Review(long id, ReviewIncidentRequest request, CancellationToken ct)
    {
        await incidents.ReviewAsync(id, request, ct);
        return OkResponse<object?>(null, "Incident under review.");
    }

    [HttpPost("{id:long}/close")]
    [HasPermission(PermissionRegistry.IncidentReview)]
    public async Task<ActionResult<ApiResponse>> Close(long id, CloseIncidentRequest request, CancellationToken ct)
    {
        await incidents.CloseAsync(id, request, ct);
        return OkResponse<object?>(null, "Incident closed.");
    }

    /// <summary>Quality's RCA action (plan §20) — records root-cause analysis and the sentinel-event flag on the same incident record Nursing reports/reviews.</summary>
    [HttpPost("{id:long}/rca")]
    [HasPermission(PermissionRegistry.QualityIncidentRca)]
    public async Task<ActionResult<ApiResponse>> RecordRca(long id, RecordIncidentRcaRequest request, CancellationToken ct)
    {
        await incidents.RecordRcaAsync(id, request, ct);
        return OkResponse<object?>(null, "Root-cause analysis recorded.");
    }
}
