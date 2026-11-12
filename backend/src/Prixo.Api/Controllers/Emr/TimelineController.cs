using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Emr;

namespace Prixo.Api.Controllers.Emr;

[Route("api/v1/emr/patients/{patientId:long}")]
public class TimelineController(TimelineService timeline) : ApiControllerBase
{
    [HttpGet("timeline")]
    [HasPermission(PermissionRegistry.EmrTimelineView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TimelineEventDto>>>> GetTimeline(long patientId, CancellationToken ct) =>
        OkResponse(await timeline.GetTimelineAsync(patientId, ct));

    [HttpGet("prescriptions")]
    [HasPermission(PermissionRegistry.EmrTimelineView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PrescriptionHistoryDto>>>> GetPrescriptions(long patientId, CancellationToken ct) =>
        OkResponse(await timeline.GetPrescriptionHistoryAsync(patientId, ct));

    [HttpGet("investigations")]
    [HasPermission(PermissionRegistry.EmrTimelineView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InvestigationHistoryDto>>>> GetInvestigations(long patientId, CancellationToken ct) =>
        OkResponse(await timeline.GetInvestigationHistoryAsync(patientId, ct));

    [HttpGet("procedures")]
    [HasPermission(PermissionRegistry.EmrTimelineView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProcedureHistoryDto>>>> GetProcedures(long patientId, CancellationToken ct) =>
        OkResponse(await timeline.GetProcedureHistoryAsync(patientId, ct));

    [HttpGet("discharge-summaries")]
    [HasPermission(PermissionRegistry.EmrTimelineView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DischargeSummaryArchiveDto>>>> GetDischargeSummaries(long patientId, CancellationToken ct) =>
        OkResponse(await timeline.GetDischargeSummaryArchiveAsync(patientId, ct));

    [HttpGet("results-trend")]
    [HasPermission(PermissionRegistry.EmrTimelineView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ResultsTrendSeriesDto>>>> GetResultsTrend(
        long patientId, [FromQuery] long testId, CancellationToken ct) =>
        OkResponse(await timeline.GetResultsTrendAsync(patientId, testId, ct));
}
