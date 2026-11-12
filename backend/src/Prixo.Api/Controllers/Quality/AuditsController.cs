using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Quality;

namespace Prixo.Api.Controllers.Quality;

[Route("api/v1/quality/audit-templates")]
public class AuditTemplatesController(QualityAuditService audits) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.QualityAuditTemplateView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AuditTemplateDto>>>> GetAll(CancellationToken ct) =>
        OkResponse(await audits.GetTemplatesAsync(ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.QualityAuditTemplateManage)]
    public async Task<ActionResult<ApiResponse<long>>> Create(SaveAuditTemplateRequest request, CancellationToken ct)
    {
        var id = await audits.CreateTemplateAsync(request, ct);
        return CreatedResponse($"/api/v1/quality/audit-templates/{id}", id, "Audit template created.");
    }
}

[Route("api/v1/quality/audits")]
public class AuditsController(QualityAuditService audits) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.QualityAuditView)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditDto>>>> GetPaged([FromQuery] AuditQuery query, CancellationToken ct) =>
        OkResponse(await audits.GetPagedAsync(query, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.QualityAuditManage)]
    public async Task<ActionResult<ApiResponse<long>>> Schedule(ScheduleAuditRequest request, CancellationToken ct)
    {
        var id = await audits.ScheduleAsync(request, ct);
        return CreatedResponse($"/api/v1/quality/audits/{id}", id, "Audit scheduled.");
    }

    [HttpPost("{id:long}/score")]
    [HasPermission(PermissionRegistry.QualityAuditManage)]
    public async Task<ActionResult<ApiResponse>> Score(long id, ScoreAuditRequest request, CancellationToken ct)
    {
        await audits.ScoreAsync(id, request, ct);
        return OkResponse<object?>(null, "Audit scored.");
    }

    [HttpPost("{id:long}/complete")]
    [HasPermission(PermissionRegistry.QualityAuditManage)]
    public async Task<ActionResult<ApiResponse>> Complete(long id, CancellationToken ct)
    {
        await audits.CompleteAsync(id, ct);
        return OkResponse<object?>(null, "Audit completed.");
    }
}
