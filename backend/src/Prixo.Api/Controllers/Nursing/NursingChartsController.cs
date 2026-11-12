using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Nursing;
using Prixo.Domain.Enums;

namespace Prixo.Api.Controllers.Nursing;

[Route("api/v1/nursing/admissions/{admissionId:long}")]
public class NursingChartsController(NursingChartService charts) : ApiControllerBase
{
    [HttpGet("assessments")]
    [HasPermission(PermissionRegistry.NursingChartView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NursingAssessmentDto>>>> GetAssessments(long admissionId, CancellationToken ct) =>
        OkResponse(await charts.GetNursingAssessmentsAsync(admissionId, ct));

    [HttpPost("assessments")]
    [HasPermission(PermissionRegistry.NursingChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddAssessment(long admissionId, AddNursingAssessmentRequest request, CancellationToken ct)
    {
        var id = await charts.AddNursingAssessmentAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/nursing/admissions/{admissionId}/assessments/{id}", id, "Nursing assessment recorded.");
    }

    [HttpGet("notes")]
    [HasPermission(PermissionRegistry.NursingChartView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NursingNoteDto>>>> GetNotes(long admissionId, CancellationToken ct) =>
        OkResponse(await charts.GetNursingNotesAsync(admissionId, ct));

    [HttpPost("notes")]
    [HasPermission(PermissionRegistry.NursingChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddNote(long admissionId, AddNursingNoteRequest request, CancellationToken ct)
    {
        var id = await charts.AddNursingNoteAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/nursing/admissions/{admissionId}/notes/{id}", id, "Nursing note added.");
    }

    [HttpGet("risk-assessments")]
    [HasPermission(PermissionRegistry.NursingChartView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RiskAssessmentDto>>>> GetRiskAssessments(
        long admissionId, [FromQuery] RiskAssessmentType? type, CancellationToken ct) =>
        OkResponse(await charts.GetRiskAssessmentsAsync(admissionId, type, ct));

    [HttpPost("risk-assessments")]
    [HasPermission(PermissionRegistry.NursingChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddRiskAssessment(long admissionId, AddRiskAssessmentRequest request, CancellationToken ct)
    {
        var id = await charts.AddRiskAssessmentAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/nursing/admissions/{admissionId}/risk-assessments/{id}", id, "Risk assessment recorded.");
    }

    [HttpGet("care-plans")]
    [HasPermission(PermissionRegistry.NursingChartView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CarePlanDto>>>> GetCarePlans(long admissionId, CancellationToken ct) =>
        OkResponse(await charts.GetCarePlansAsync(admissionId, ct));

    [HttpPost("care-plans")]
    [HasPermission(PermissionRegistry.NursingChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddCarePlan(long admissionId, AddCarePlanRequest request, CancellationToken ct)
    {
        var id = await charts.AddCarePlanAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/nursing/admissions/{admissionId}/care-plans/{id}", id, "Care plan created.");
    }

    [HttpPost("care-plans/{carePlanId:long}/resolve")]
    [HasPermission(PermissionRegistry.NursingChartManage)]
    public async Task<ActionResult<ApiResponse>> ResolveCarePlan(long admissionId, long carePlanId, ResolveCarePlanRequest request, CancellationToken ct)
    {
        await charts.ResolveCarePlanAsync(admissionId, carePlanId, request, ct);
        return OkResponse<object?>(null, "Care plan resolved.");
    }

    [HttpGet("transfer-notes")]
    [HasPermission(PermissionRegistry.NursingChartView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TransferNoteDto>>>> GetTransferNotes(long admissionId, CancellationToken ct) =>
        OkResponse(await charts.GetTransferNotesAsync(admissionId, ct));

    [HttpPost("transfer-notes")]
    [HasPermission(PermissionRegistry.NursingChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddTransferNote(long admissionId, AddTransferNoteRequest request, CancellationToken ct)
    {
        var id = await charts.AddTransferNoteAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/nursing/admissions/{admissionId}/transfer-notes/{id}", id, "Transfer note added.");
    }
}
