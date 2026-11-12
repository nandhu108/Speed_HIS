using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Icu;

namespace Prixo.Api.Controllers.Icu;

[Route("api/v1/icu")]
public class IcuChartController(IcuChartService chart) : ApiControllerBase
{
    // ── Ventilator ───────────────────────────────────────────────────────────
    [HttpGet("admissions/{admissionId:long}/ventilator-sessions")]
    [HasPermission(PermissionRegistry.IcuChartView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VentilatorSessionDto>>>> GetVentilatorSessions(long admissionId, CancellationToken ct) =>
        OkResponse(await chart.GetVentilatorSessionsAsync(admissionId, ct));

    [HttpPost("admissions/{admissionId:long}/ventilator-sessions")]
    [HasPermission(PermissionRegistry.IcuChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> StartVentilatorSession(long admissionId, StartVentilatorSessionRequest request, CancellationToken ct)
    {
        var id = await chart.StartVentilatorSessionAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/icu/ventilator-sessions/{id}", id, "Ventilator session started.");
    }

    [HttpPost("ventilator-sessions/{sessionId:long}/stop")]
    [HasPermission(PermissionRegistry.IcuChartManage)]
    public async Task<ActionResult<ApiResponse>> StopVentilatorSession(long sessionId, CancellationToken ct)
    {
        await chart.StopVentilatorSessionAsync(sessionId, ct);
        return OkResponse<object?>(null, "Ventilator session stopped.");
    }

    [HttpPost("ventilator-sessions/{sessionId:long}/observations")]
    [HasPermission(PermissionRegistry.IcuChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddVentilatorObservation(long sessionId, AddVentilatorObservationRequest request, CancellationToken ct)
    {
        var id = await chart.AddVentilatorObservationAsync(sessionId, request, ct);
        return CreatedResponse($"/api/v1/icu/ventilator-sessions/{sessionId}/observations/{id}", id, "Observation recorded.");
    }

    // ── Infusion ─────────────────────────────────────────────────────────────
    [HttpGet("admissions/{admissionId:long}/infusions")]
    [HasPermission(PermissionRegistry.IcuChartView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InfusionChartEntryDto>>>> GetInfusions(long admissionId, CancellationToken ct) =>
        OkResponse(await chart.GetInfusionsAsync(admissionId, ct));

    [HttpPost("admissions/{admissionId:long}/infusions")]
    [HasPermission(PermissionRegistry.IcuChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> StartInfusion(long admissionId, StartInfusionRequest request, CancellationToken ct)
    {
        var id = await chart.StartInfusionAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/icu/infusions/{id}", id, "Infusion started.");
    }

    [HttpPost("infusions/{entryId:long}/stop")]
    [HasPermission(PermissionRegistry.IcuChartManage)]
    public async Task<ActionResult<ApiResponse>> StopInfusion(long entryId, CancellationToken ct)
    {
        await chart.StopInfusionAsync(entryId, ct);
        return OkResponse<object?>(null, "Infusion stopped.");
    }

    [HttpPost("infusions/{entryId:long}/rates")]
    [HasPermission(PermissionRegistry.IcuChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddInfusionRate(long entryId, AddInfusionRateRequest request, CancellationToken ct)
    {
        var id = await chart.AddInfusionRateAsync(entryId, request, ct);
        return CreatedResponse($"/api/v1/icu/infusions/{entryId}/rates/{id}", id, "Rate recorded.");
    }

    // ── Severity ─────────────────────────────────────────────────────────────
    [HttpGet("admissions/{admissionId:long}/severity")]
    [HasPermission(PermissionRegistry.IcuChartView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IcuSeverityAssessmentDto>>>> GetSeverityHistory(long admissionId, CancellationToken ct) =>
        OkResponse(await chart.GetSeverityHistoryAsync(admissionId, ct));

    [HttpPost("admissions/{admissionId:long}/severity")]
    [HasPermission(PermissionRegistry.IcuChartManage)]
    public async Task<ActionResult<ApiResponse<long>>> RecordSeverity(long admissionId, RecordSeverityRequest request, CancellationToken ct)
    {
        var id = await chart.RecordSeverityAsync(admissionId, request, ct);
        return CreatedResponse($"/api/v1/icu/admissions/{admissionId}/severity/{id}", id, "Severity recorded.");
    }
}
