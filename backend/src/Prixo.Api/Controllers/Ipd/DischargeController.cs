using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Ipd;
using Prixo.Domain.Enums;

namespace Prixo.Api.Controllers.Ipd;

[Route("api/v1/ipd/admissions/{admissionId:long}/discharge")]
public class DischargeController(DischargeService discharge) : ApiControllerBase
{
    [HttpPost("request")]
    [HasPermission(PermissionRegistry.DischargeRequest)]
    public async Task<ActionResult<ApiResponse>> RequestDischarge(long admissionId, RequestDischargeRequest request, CancellationToken ct)
    {
        await discharge.RequestDischargeAsync(admissionId, request, ct);
        return OkResponse<object?>(null, "Discharge requested.");
    }

    [HttpGet("clearances")]
    [HasPermission(PermissionRegistry.DischargeRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DischargeClearanceDto>>>> GetClearances(long admissionId, CancellationToken ct) =>
        OkResponse(await discharge.GetClearancesAsync(admissionId, ct));

    [HttpPost("clearances/{clearanceType}")]
    [HasPermission(PermissionRegistry.DischargeClearItem)]
    public async Task<ActionResult<ApiResponse>> ClearChecklistItem(
        long admissionId, ClearanceType clearanceType, ClearChecklistItemRequest request, CancellationToken ct)
    {
        await discharge.ClearChecklistItemAsync(admissionId, clearanceType, request, ct);
        return OkResponse<object?>(null, "Checklist item cleared.");
    }

    [HttpGet("summary")]
    [HasPermission(PermissionRegistry.DischargeRequest)]
    public async Task<ActionResult<ApiResponse<DischargeSummaryDto?>>> GetSummary(long admissionId, CancellationToken ct) =>
        OkResponse(await discharge.GetDischargeSummaryAsync(admissionId, ct));

    [HttpPut("summary")]
    [HasPermission(PermissionRegistry.DischargeRequest)]
    public async Task<ActionResult<ApiResponse<long>>> UpsertSummary(long admissionId, UpsertDischargeSummaryRequest request, CancellationToken ct) =>
        OkResponse(await discharge.UpsertDischargeSummaryAsync(admissionId, request, ct), "Discharge summary saved.");

    [HttpPost("summary/approve")]
    [HasPermission(PermissionRegistry.DischargeApproveSummary)]
    public async Task<ActionResult<ApiResponse>> ApproveSummary(long admissionId, ApproveDischargeSummaryRequest request, CancellationToken ct)
    {
        await discharge.ApproveDischargeSummaryAsync(admissionId, request, ct);
        return OkResponse<object?>(null, "Discharge summary approved.");
    }

    [HttpPost("finalize")]
    [HasPermission(PermissionRegistry.DischargeFinalize)]
    public async Task<ActionResult<ApiResponse>> Finalize(long admissionId, CancellationToken ct)
    {
        await discharge.FinalizeDischargeAsync(admissionId, ct);
        return OkResponse<object?>(null, "Patient discharged.");
    }

    [HttpGet("summary-pdf")]
    [HasPermission(PermissionRegistry.DischargeRequest)]
    public async Task<IActionResult> GetSummaryPdf(long admissionId, CancellationToken ct)
    {
        var pdf = await discharge.GenerateDischargeSummaryPdfAsync(admissionId, ct);
        return File(pdf, "application/pdf", $"discharge-summary-{admissionId}.pdf");
    }

    [HttpGet("slip")]
    [HasPermission(PermissionRegistry.DischargeRequest)]
    public async Task<IActionResult> GetSlip(long admissionId, CancellationToken ct)
    {
        var pdf = await discharge.GenerateDischargeSlipAsync(admissionId, ct);
        return File(pdf, "application/pdf", $"discharge-slip-{admissionId}.pdf");
    }
}
