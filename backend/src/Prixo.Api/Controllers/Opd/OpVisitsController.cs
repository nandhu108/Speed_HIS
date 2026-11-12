using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Opd;
using Prixo.Domain.Exceptions;

namespace Prixo.Api.Controllers.Opd;

[Route("api/v1/opd/visits")]
public class OpVisitsController(OpVisitService visits) : ApiControllerBase
{
    private const long MaxImportBytes = 5 * 1024 * 1024;

    /// <summary>"Camp Bulk Import" — one camp/doctor for the whole batch, a CSV of UHIDs per row.</summary>
    [HttpPost("import-camp")]
    [HasPermission(PermissionRegistry.VisitImport)]
    [RequestSizeLimit(MaxImportBytes)]
    public async Task<ActionResult<ApiResponse<BulkImportResultDto>>> ImportCamp(
        [FromForm] long campId, [FromForm] long doctorId, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            throw new DomainException("The uploaded file is empty.");
        }
        await using var stream = file.OpenReadStream();
        var result = await visits.ImportCampVisitsAsync(campId, doctorId, stream, ct);
        return OkResponse(result, $"Import complete: {result.Inserted} registered, {result.Skipped} skipped.");
    }

    [HttpGet]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<ActionResult<ApiResponse<PagedResult<OpVisitListItemDto>>>> GetPaged(
        [FromQuery] OpVisitQuery query, CancellationToken ct) =>
        OkResponse(await visits.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<ActionResult<ApiResponse<OpVisitDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await visits.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(PermissionRegistry.VisitCreate)]
    public async Task<ActionResult<ApiResponse<OpVisitDetailDto>>> Register(
        RegisterOpVisitRequest request, CancellationToken ct)
    {
        var result = await visits.RegisterAsync(request, ct);
        return CreatedResponse($"/api/v1/opd/visits/{result.Id}", result, $"Visit registered — token #{result.TokenNumber}.");
    }

    [HttpGet("{id:long}/workspace")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse<ConsultationWorkspaceDto>>> GetWorkspace(long id, CancellationToken ct) =>
        OkResponse(await visits.GetWorkspaceAsync(id, ct));

    [HttpPost("{id:long}/start-consult")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse>> StartConsult(long id, CancellationToken ct)
    {
        await visits.StartConsultAsync(id, ct);
        return OkResponse<object?>(null, "Consultation started.");
    }

    [HttpPost("{id:long}/complete")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse>> Complete(long id, CancellationToken ct)
    {
        await visits.CompleteAsync(id, ct);
        return OkResponse<object?>(null, "Visit completed.");
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, CancelVisitRequest request, CancellationToken ct)
    {
        await visits.CancelAsync(id, request, ct);
        return OkResponse<object?>(null, "Visit cancelled.");
    }

    /// <summary>Front-desk correction of a still-waiting visit's Camp tag (e.g. "Camp Registration
    /// Modify") — deliberately gated on VisitCreate (front desk's own permission), not the
    /// coarser VisitManage which also covers clinical workspace entries.</summary>
    [HttpPut("{id:long}/camp")]
    [HasPermission(PermissionRegistry.VisitCreate)]
    public async Task<ActionResult<ApiResponse>> UpdateCamp(long id, UpdateVisitCampRequest request, CancellationToken ct)
    {
        await visits.UpdateCampAsync(id, request, ct);
        return OkResponse<object?>(null, "Camp updated.");
    }

    [HttpPut("{id:long}/vitals")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse>> SaveVitals(long id, SaveVitalsRequest request, CancellationToken ct)
    {
        await visits.SaveVitalsAsync(id, request, ct);
        return OkResponse<object?>(null, "Vitals saved.");
    }

    [HttpPut("{id:long}/complaints")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse>> SaveComplaints(long id, SaveComplaintsRequest request, CancellationToken ct)
    {
        await visits.SaveComplaintsAsync(id, request, ct);
        return OkResponse<object?>(null, "Complaints saved.");
    }

    [HttpPut("{id:long}/examination")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse>> SaveExamination(long id, SaveExaminationRequest request, CancellationToken ct)
    {
        await visits.SaveExaminationAsync(id, request, ct);
        return OkResponse<object?>(null, "Examination saved.");
    }

    [HttpPut("{id:long}/diagnoses")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse>> SaveDiagnoses(long id, SaveDiagnosesRequest request, CancellationToken ct)
    {
        await visits.SaveDiagnosesAsync(id, request, ct);
        return OkResponse<object?>(null, "Diagnoses saved.");
    }

    [HttpPut("{id:long}/prescription")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse>> SavePrescription(long id, SavePrescriptionRequest request, CancellationToken ct)
    {
        await visits.SavePrescriptionAsync(id, request, ct);
        return OkResponse<object?>(null, "Prescription saved.");
    }

    [HttpGet("{id:long}/orders")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClinicalOrderDto>>>> GetOrders(long id, CancellationToken ct) =>
        OkResponse(await visits.GetOrdersAsync(id, ct));

    [HttpPost("{id:long}/orders")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddOrder(long id, AddClinicalOrderRequest request, CancellationToken ct)
    {
        var orderId = await visits.AddOrderAsync(id, request, ct);
        return CreatedResponse($"/api/v1/opd/visits/{id}/orders/{orderId}", orderId, "Order added.");
    }

    [HttpPost("{id:long}/orders/{orderId:long}/cancel")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse>> CancelOrder(long id, long orderId, CancellationToken ct)
    {
        await visits.CancelOrderAsync(id, orderId, ct);
        return OkResponse<object?>(null, "Order cancelled.");
    }

    [HttpGet("{id:long}/referrals")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VisitReferralDto>>>> GetReferrals(long id, CancellationToken ct) =>
        OkResponse(await visits.GetReferralsAsync(id, ct));

    [HttpPost("{id:long}/referrals")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddReferral(long id, AddVisitReferralRequest request, CancellationToken ct)
    {
        var referralId = await visits.AddReferralAsync(id, request, ct);
        return CreatedResponse($"/api/v1/opd/visits/{id}/referrals/{referralId}", referralId, "Referral added.");
    }

    [HttpDelete("{id:long}/referrals/{referralId:long}")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse>> DeleteReferral(long id, long referralId, CancellationToken ct)
    {
        await visits.DeleteReferralAsync(id, referralId, ct);
        return OkResponse<object?>(null, "Referral removed.");
    }

    /// <summary>"Doctor Opinion Entry" — records the referred-to doctor's cross-consult response
    /// directly on the visit's Referral tab (staff-facing; the Doctor Portal's own inbox wraps
    /// this same action with an ownership check).</summary>
    [HttpPut("{id:long}/referrals/{referralId:long}/opinion")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse>> SaveOpinion(
        long id, long referralId, SaveReferralOpinionRequest request, CancellationToken ct)
    {
        await visits.SaveOpinionAsync(id, referralId, request, ct);
        return OkResponse<object?>(null, "Opinion recorded.");
    }

    [HttpPut("{id:long}/follow-up")]
    [HasPermission(PermissionRegistry.VisitManage)]
    public async Task<ActionResult<ApiResponse>> SaveFollowUp(long id, SaveFollowUpRequest request, CancellationToken ct)
    {
        await visits.SaveFollowUpAsync(id, request, ct);
        return OkResponse<object?>(null, "Follow-up advice saved.");
    }

    [HttpGet("{id:long}/slip")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<IActionResult> GetSlip(long id, CancellationToken ct)
    {
        var pdf = await visits.GenerateVisitSlipAsync(id, ct);
        return File(pdf, "application/pdf", $"visit-slip-{id}.pdf");
    }

    [HttpGet("{id:long}/summary")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<IActionResult> GetSummary(long id, CancellationToken ct)
    {
        var pdf = await visits.GenerateVisitSummaryAsync(id, ct);
        return File(pdf, "application/pdf", $"visit-summary-{id}.pdf");
    }

    /// <summary>Today's (or a given date's) OPD queue, per the plan's `/opd/queue?doctorId` map.</summary>
    [HttpGet("/api/v1/opd/queue")]
    [HasPermission(PermissionRegistry.VisitView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<QueueEntryDto>>>> GetQueue(
        [FromQuery] long? doctorId, [FromQuery] DateOnly? date, CancellationToken ct) =>
        OkResponse(await visits.GetQueueAsync(doctorId, date ?? DateOnly.FromDateTime(DateTime.UtcNow), ct));
}
