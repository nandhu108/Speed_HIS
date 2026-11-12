using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Quality;

namespace Prixo.Api.Controllers.Quality;

[Route("api/v1/quality/dashboards")]
public class QualityDashboardsController(QualityDashboardService dashboards) : ApiControllerBase
{
    [HttpGet("kpi")]
    [HasPermission(PermissionRegistry.QualityDashboardView)]
    public async Task<ActionResult<ApiResponse<KpiDashboardDto>>> GetKpi(
        [FromQuery] DateOnly fromPeriod, [FromQuery] DateOnly toPeriod, CancellationToken ct) =>
        OkResponse(await dashboards.GetKpiDashboardAsync(fromPeriod, toPeriod, ct));

    [HttpGet("compliance")]
    [HasPermission(PermissionRegistry.QualityDashboardView)]
    public async Task<ActionResult<ApiResponse<ComplianceDashboardDto>>> GetCompliance(CancellationToken ct) =>
        OkResponse(await dashboards.GetComplianceDashboardAsync(ct));
}
