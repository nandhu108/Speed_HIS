using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Biomed;

namespace Prixo.Api.Controllers.Biomed;

[Route("api/v1/biomed/reports")]
public class BiomedReportsController(BiomedReportService reports) : ApiControllerBase
{
    [HttpGet("downtime")]
    [HasPermission(PermissionRegistry.BiomedReportView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DowntimeReportRowDto>>>> GetDowntime([FromQuery] BiomedReportDateRange range, CancellationToken ct) =>
        OkResponse(await reports.GetDowntimeReportAsync(range, ct));

    [HttpGet("pm-compliance")]
    [HasPermission(PermissionRegistry.BiomedReportView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PmComplianceRowDto>>>> GetPmCompliance([FromQuery] BiomedReportDateRange range, CancellationToken ct) =>
        OkResponse(await reports.GetPmComplianceReportAsync(range, ct));
}
