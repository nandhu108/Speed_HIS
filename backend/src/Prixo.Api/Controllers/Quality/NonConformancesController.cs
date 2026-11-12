using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Quality;

namespace Prixo.Api.Controllers.Quality;

[Route("api/v1/quality/non-conformances")]
public class NonConformancesController(QualityNonConformanceService nonConformances) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.QualityNonConformanceView)]
    public async Task<ActionResult<ApiResponse<PagedResult<NonConformanceDto>>>> GetPaged(
        [FromQuery] NonConformanceQuery query, CancellationToken ct) =>
        OkResponse(await nonConformances.GetPagedAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.QualityNonConformanceCreate)]
    public async Task<ActionResult<ApiResponse<long>>> Raise(RaiseNonConformanceRequest request, CancellationToken ct)
    {
        var id = await nonConformances.RaiseAsync(request, ct);
        return CreatedResponse($"/api/v1/quality/non-conformances/{id}", id, "Non-conformance raised.");
    }

    [HttpPost("{id:long}/in-progress")]
    [HasPermission(PermissionRegistry.QualityNonConformanceManage)]
    public async Task<ActionResult<ApiResponse>> MarkInProgress(long id, CancellationToken ct)
    {
        await nonConformances.MarkInProgressAsync(id, ct);
        return OkResponse<object?>(null, "Non-conformance in progress.");
    }

    [HttpPost("{id:long}/close")]
    [HasPermission(PermissionRegistry.QualityNonConformanceManage)]
    public async Task<ActionResult<ApiResponse>> Close(long id, CloseNonConformanceRequest request, CancellationToken ct)
    {
        await nonConformances.CloseAsync(id, request, ct);
        return OkResponse<object?>(null, "Non-conformance closed.");
    }
}
