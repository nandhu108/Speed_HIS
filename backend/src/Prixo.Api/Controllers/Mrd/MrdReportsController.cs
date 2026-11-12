using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Mrd;

namespace Prixo.Api.Controllers.Mrd;

[Route("api/v1/mrd/reports")]
public class MrdReportsController(MrdReportService reports) : ApiControllerBase
{
    [HttpGet("coding-stats")]
    [HasPermission(PermissionRegistry.MrdReportView)]
    public async Task<ActionResult<ApiResponse<CodingStatsDto>>> GetCodingStats([FromQuery] MrdReportDateRange range, CancellationToken ct) =>
        OkResponse(await reports.GetCodingStatsAsync(range, ct));

    [HttpGet("file-movement")]
    [HasPermission(PermissionRegistry.MrdReportView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FileMovementReportRowDto>>>> GetFileMovement([FromQuery] MrdReportDateRange range, CancellationToken ct) =>
        OkResponse(await reports.GetFileMovementReportAsync(range, ct));

    [HttpGet("incomplete-records")]
    [HasPermission(PermissionRegistry.MrdReportView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IncompleteRecordRowDto>>>> GetIncompleteRecords([FromQuery] MrdReportDateRange range, CancellationToken ct) =>
        OkResponse(await reports.GetIncompleteRecordsAsync(range, ct));
}
