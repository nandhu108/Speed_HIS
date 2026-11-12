using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Quality;
using Prixo.Domain.Enums;

namespace Prixo.Api.Controllers.Quality;

[Route("api/v1/quality/risks")]
public class RisksController(QualityRiskService risks) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.QualityRiskView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RiskDto>>>> GetAll([FromQuery] RiskQuery query, CancellationToken ct) =>
        OkResponse(await risks.GetAllAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.QualityRiskManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveRiskRequest request, CancellationToken ct)
    {
        var id = await risks.CreateAsync(request, ct);
        return CreatedResponse($"/api/v1/quality/risks/{id}", id, "Risk recorded.");
    }

    [HttpPost("{id:long}/status")]
    [HasPermission(PermissionRegistry.QualityRiskManage)]
    public async Task<ActionResult<ApiResponse>> SetStatus(long id, [FromQuery] RiskStatus status, CancellationToken ct)
    {
        await risks.SetStatusAsync(id, status, ct);
        return OkResponse<object?>(null, "Risk status updated.");
    }
}
