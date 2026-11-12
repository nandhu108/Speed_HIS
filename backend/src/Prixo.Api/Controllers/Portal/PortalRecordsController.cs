using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Emr;
using Prixo.Application.Features.Portal;

namespace Prixo.Api.Controllers.Portal;

/// <summary>Portal Records (plan §21) — one controller for the whole "my records" surface, since every action is a thin read/PDF pass-through on PortalRecordsService.</summary>
[Route("api/v1/portal")]
[PortalAuth]
public class PortalRecordsController(PortalRecordsService records) : ApiControllerBase
{
    [HttpGet("lab-reports")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PortalLabOrderDto>>>> GetLabOrders(CancellationToken ct) =>
        OkResponse(await records.GetLabOrdersAsync(ct));

    [HttpGet("lab-reports/{orderId:long}/pdf")]
    public async Task<IActionResult> DownloadLabReport(long orderId, CancellationToken ct) =>
        File(await records.GetLabReportPdfAsync(orderId, ct), "application/pdf", $"lab-report-{orderId}.pdf");

    [HttpGet("rad-reports")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PortalRadOrderItemDto>>>> GetRadOrders(CancellationToken ct) =>
        OkResponse(await records.GetRadOrderItemsAsync(ct));

    [HttpGet("rad-reports/{itemId:long}/pdf")]
    public async Task<IActionResult> DownloadRadReport(long itemId, CancellationToken ct) =>
        File(await records.GetRadReportPdfAsync(itemId, ct), "application/pdf", $"radiology-report-{itemId}.pdf");

    [HttpGet("prescriptions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PortalPrescriptionDto>>>> GetPrescriptions(CancellationToken ct) =>
        OkResponse(await records.GetPrescriptionsAsync(ct));

    [HttpGet("bills")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PortalBillDto>>>> GetBills(CancellationToken ct) =>
        OkResponse(await records.GetBillsAsync(ct));

    [HttpGet("bills/{billId:long}/receipt")]
    public async Task<IActionResult> DownloadReceipt(long billId, CancellationToken ct) =>
        File(await records.GetBillReceiptPdfAsync(billId, ct), "application/pdf", $"receipt-{billId}.pdf");

    /// <summary>Payment placeholder (plan §21: "gateway held") — queues intent only, see PortalRecordsService.RequestOnlinePaymentAsync.</summary>
    [HttpPost("bills/{billId:long}/pay-online")]
    public async Task<ActionResult<ApiResponse<OnlinePaymentRequestedDto>>> RequestOnlinePayment(long billId, CancellationToken ct) =>
        OkResponse(await records.RequestOnlinePaymentAsync(billId, ct), "Online payment requested — our billing team will contact you to complete it.");

    [HttpGet("discharge-summaries")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PortalDischargeSummaryDto>>>> GetDischargeSummaries(CancellationToken ct) =>
        OkResponse(await records.GetDischargeSummariesAsync(ct));

    [HttpGet("discharge-summaries/{admissionId:long}/pdf")]
    public async Task<IActionResult> DownloadDischargeSummary(long admissionId, CancellationToken ct) =>
        File(await records.GetDischargeSummaryPdfAsync(admissionId, ct), "application/pdf", $"discharge-summary-{admissionId}.pdf");

    [HttpGet("medical-history")]
    public async Task<ActionResult<ApiResponse<PortalMedicalHistoryDto>>> GetMedicalHistory(CancellationToken ct) =>
        OkResponse(new PortalMedicalHistoryDto(
            await records.GetMedicalHistoryAsync(ct), await records.GetSurgicalHistoryAsync(ct),
            await records.GetAllergiesAsync(ct), await records.GetImmunizationsAsync(ct)));
}

public record PortalMedicalHistoryDto(
    IReadOnlyList<MedicalHistoryDto> Medical, IReadOnlyList<SurgicalHistoryDto> Surgical,
    IReadOnlyList<AllergyDto> Allergies, IReadOnlyList<ImmunizationDto> Immunizations);
