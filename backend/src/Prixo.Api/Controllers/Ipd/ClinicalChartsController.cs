using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Ipd;

namespace Prixo.Api.Controllers.Ipd;

[Route("api/v1/ipd/admissions/{admissionId:long}")]
public class ClinicalChartsController(ClinicalChartService charts) : ApiControllerBase
{
    [HttpGet("progress-notes")]
    [HasPermission(PermissionRegistry.ChartView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProgressNoteDto>>>> GetProgressNotes(long admissionId, CancellationToken ct) =>
        OkResponse(await charts.GetProgressNotesAsync(admissionId, ct));

    [HttpPost("progress-notes")]
    [HasPermission(PermissionRegistry.ChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddProgressNote(long admissionId, AddProgressNoteRequest request, CancellationToken ct)
    {
        var id = await charts.AddProgressNoteAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/ipd/admissions/{admissionId}/progress-notes/{id}", id, "Progress note added.");
    }

    [HttpGet("vitals")]
    [HasPermission(PermissionRegistry.ChartView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IpVitalsDto>>>> GetVitals(long admissionId, CancellationToken ct) =>
        OkResponse(await charts.GetVitalsAsync(admissionId, ct));

    [HttpPost("vitals")]
    [HasPermission(PermissionRegistry.ChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddVitals(long admissionId, AddVitalsRequest request, CancellationToken ct)
    {
        var id = await charts.AddVitalsAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/ipd/admissions/{admissionId}/vitals/{id}", id, "Vitals recorded.");
    }

    [HttpGet("intake-output")]
    [HasPermission(PermissionRegistry.ChartView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IntakeOutputDto>>>> GetIntakeOutput(long admissionId, CancellationToken ct) =>
        OkResponse(await charts.GetIntakeOutputAsync(admissionId, ct));

    [HttpPost("intake-output")]
    [HasPermission(PermissionRegistry.ChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddIntakeOutput(long admissionId, AddIntakeOutputRequest request, CancellationToken ct)
    {
        var id = await charts.AddIntakeOutputAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/ipd/admissions/{admissionId}/intake-output/{id}", id, "Intake/output recorded.");
    }

    [HttpGet("procedure-notes")]
    [HasPermission(PermissionRegistry.ChartView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProcedureNoteDto>>>> GetProcedureNotes(long admissionId, CancellationToken ct) =>
        OkResponse(await charts.GetProcedureNotesAsync(admissionId, ct));

    [HttpPost("procedure-notes")]
    [HasPermission(PermissionRegistry.ChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddProcedureNote(long admissionId, AddProcedureNoteRequest request, CancellationToken ct)
    {
        var id = await charts.AddProcedureNoteAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/ipd/admissions/{admissionId}/procedure-notes/{id}", id, "Procedure note added.");
    }
}
