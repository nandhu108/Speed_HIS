using Microsoft.AspNetCore.Mvc;
using Prixo.Api.Authorization;
using Prixo.Api.Controllers.Reports;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Common.Security;
using Prixo.Application.Features.Billing;
using Prixo.Application.Features.Reports;

namespace Prixo.Api.Controllers.Billing;

[Route("api/v1/billing/bills")]
public class BillsController(BillingService billing, IReportExportService excel, IPdfService pdf) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionRegistry.BillView)]
    public async Task<ActionResult<ApiResponse<PagedResult<BillListItemDto>>>> GetPaged(
        [FromQuery] BillQuery query, CancellationToken ct) =>
        OkResponse(await billing.GetPagedAsync(query, ct));

    /// <summary>OP Billing's own patient search (legacy "OP Bill Payment" search) — lists OP/MHC
    /// visits to click into and generate/open a bill for, distinct from Patient Registration's
    /// patient search.</summary>
    [HttpGet("op-search")]
    [HasPermission(PermissionRegistry.BillView)]
    public async Task<ActionResult<ApiResponse<PagedResult<OpBillingSearchResultDto>>>> SearchOpVisits(
        [FromQuery] OpBillingSearchQuery query, CancellationToken ct) =>
        OkResponse(await billing.SearchOpVisitsForBillingAsync(query, ct));

    /// <summary>"OP Bill Report" / "IP Bill Generated Report" — same filters as the paged list, unpaged, exported as CSV/Excel/PDF.</summary>
    [HttpGet("export")]
    [HasPermission(PermissionRegistry.BillView)]
    public async Task<IActionResult> Export([FromQuery] BillQuery query, [FromQuery] string format, CancellationToken ct)
    {
        var rows = await billing.GetForReportAsync(query, ct);
        var columns = new List<ReportColumnDto>
        {
            new("billNumber", "Bill No"), new("billDate", "Date"), new("patientUhid", "UHID"), new("patientName", "Patient"),
            new("billType", "Type"), new("status", "Status"), new("totalAmount", "Total"), new("paidAmount", "Paid"), new("balanceAmount", "Balance"),
        };
        var dictRows = rows.Select(r => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["billNumber"] = r.BillNumber, ["billDate"] = r.BillDate.ToString("yyyy-MM-dd"), ["patientUhid"] = r.PatientUhid,
            ["patientName"] = r.PatientName, ["billType"] = r.BillType.ToString(), ["status"] = r.Status.ToString(),
            ["totalAmount"] = r.TotalAmount, ["paidAmount"] = r.PaidAmount, ["balanceAmount"] = r.BalanceAmount,
        }).ToList();

        var result = new ReportResultDto("Bill Report", columns, dictRows);
        return ReportExportHelper.ToFileResult(result, format, "bill-report", ReportExportHelper.DateRangeLabel(query.FromDate, query.ToDate), excel, pdf);
    }

    [HttpGet("{id:long}")]
    [HasPermission(PermissionRegistry.BillView)]
    public async Task<ActionResult<ApiResponse<BillDetailDto>>> GetById(long id, CancellationToken ct) =>
        OkResponse(await billing.GetByIdAsync(id, ct));

    [HttpPost("generate-op")]
    [HasPermission(PermissionRegistry.BillCreateOp)]
    public async Task<ActionResult<ApiResponse<BillDetailDto>>> GenerateOpBill(
        GenerateOpBillRequest request, CancellationToken ct)
    {
        var result = await billing.GenerateOpBillAsync(request, ct);
        return CreatedResponse($"/api/v1/billing/bills/{result.Id}", result, $"Bill {result.BillNumber} generated.");
    }

    [HttpPost("generate-ip/{admissionId:long}")]
    [HasPermission(PermissionRegistry.BillCreateIp)]
    public async Task<ActionResult<ApiResponse<BillDetailDto>>> GenerateIpBill(long admissionId, CancellationToken ct)
    {
        var result = await billing.GenerateIpBillAsync(admissionId, ct);
        return CreatedResponse($"/api/v1/billing/bills/{result.Id}", result, $"Bill {result.BillNumber} generated.");
    }

    [HttpPost("generate-emergency/{emergencyVisitId:long}")]
    [HasPermission(PermissionRegistry.BillCreateEmergency)]
    public async Task<ActionResult<ApiResponse<BillDetailDto>>> GenerateEmergencyBill(long emergencyVisitId, CancellationToken ct)
    {
        var result = await billing.GenerateEmergencyBillAsync(emergencyVisitId, ct);
        return CreatedResponse($"/api/v1/billing/bills/{result.Id}", result, $"Bill {result.BillNumber} generated.");
    }

    [HttpPost("{id:long}/refresh-bed-charges")]
    [HasPermission(PermissionRegistry.BillManage)]
    public async Task<ActionResult<ApiResponse<BillDetailDto>>> RefreshBedCharges(long id, CancellationToken ct) =>
        OkResponse(await billing.RefreshBedChargesAsync(id, ct), "Bed charges refreshed.");

    [HttpPost("{id:long}/pending-orders")]
    [HasPermission(PermissionRegistry.BillManage)]
    public async Task<ActionResult<ApiResponse<BillDetailDto>>> AddPendingOrders(long id, CancellationToken ct) =>
        OkResponse(await billing.AddPendingOrdersAsync(id, ct), "Pending lab/radiology orders added to the bill.");

    [HttpPost("{id:long}/items")]
    [HasPermission(PermissionRegistry.BillManage)]
    public async Task<ActionResult<ApiResponse<long>>> AddItem(long id, AddManualBillItemRequest request, CancellationToken ct)
    {
        var itemId = await billing.AddManualItemAsync(id, request, ct);
        return CreatedResponse($"/api/v1/billing/bills/{id}/items/{itemId}", itemId, "Line item added.");
    }

    [HttpPut("{id:long}/items/{itemId:long}")]
    [HasPermission(PermissionRegistry.BillManage)]
    public async Task<ActionResult<ApiResponse>> UpdateItem(long id, long itemId, UpdateBillItemRequest request, CancellationToken ct)
    {
        await billing.UpdateItemAsync(id, itemId, request, ct);
        return OkResponse<object?>(null, "Line item updated.");
    }

    [HttpDelete("{id:long}/items/{itemId:long}")]
    [HasPermission(PermissionRegistry.BillManage)]
    public async Task<ActionResult<ApiResponse>> RemoveItem(long id, long itemId, CancellationToken ct)
    {
        await billing.RemoveItemAsync(id, itemId, ct);
        return OkResponse<object?>(null, "Line item removed.");
    }

    [HttpPost("{id:long}/items/{itemId:long}/discount")]
    [HasPermission(PermissionRegistry.BillManage)]
    public async Task<ActionResult<ApiResponse<DiscountApprovalDto?>>> ApplyItemDiscount(
        long id, long itemId, ApplyItemDiscountRequest request, CancellationToken ct)
    {
        var approval = await billing.ApplyItemDiscountAsync(id, itemId, request, ct);
        return OkResponse(approval, approval is null ? "Discount applied." : "Discount routed for approval.");
    }

    [HttpPost("{id:long}/discount")]
    [HasPermission(PermissionRegistry.BillManage)]
    public async Task<ActionResult<ApiResponse<DiscountApprovalDto?>>> ApplyBillDiscount(
        long id, ApplyBillDiscountRequest request, CancellationToken ct)
    {
        var approval = await billing.ApplyBillDiscountAsync(id, request, ct);
        return OkResponse(approval, approval is null ? "Discount applied." : "Discount routed for approval.");
    }

    [HttpPost("{id:long}/payments")]
    [HasPermission(PermissionRegistry.PaymentCollect)]
    public async Task<ActionResult<ApiResponse<BillPaymentDto>>> CollectPayment(
        long id, CollectPaymentRequest request, CancellationToken ct)
    {
        var payment = await billing.CollectPaymentAsync(id, request, ct);
        return OkResponse(payment, $"Payment collected — receipt {payment.ReceiptNumber}.");
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionRegistry.BillCancel)]
    public async Task<ActionResult<ApiResponse>> Cancel(long id, CancelBillRequest request, CancellationToken ct)
    {
        await billing.CancelAsync(id, request, ct);
        return OkResponse<object?>(null, "Bill cancelled.");
    }

    [HttpPost("{id:long}/credit")]
    [HasPermission(PermissionRegistry.BillManage)]
    public async Task<ActionResult<ApiResponse>> MakeCredit(long id, MakeCreditBillRequest request, CancellationToken ct)
    {
        await billing.MakeCreditBillAsync(id, request, ct);
        return OkResponse<object?>(null, "Bill marked as credit.");
    }

    [HttpPost("{id:long}/refunds")]
    [HasPermission(PermissionRegistry.RefundRequest)]
    public async Task<ActionResult<ApiResponse<RefundDto>>> RequestRefund(long id, RequestRefundRequest request, CancellationToken ct)
    {
        var refund = await billing.RequestRefundAsync(id, request, ct);
        return CreatedResponse($"/api/v1/billing/refunds/{refund.Id}", refund, "Refund requested.");
    }

    [HttpGet("{id:long}/receipt")]
    [HasPermission(PermissionRegistry.BillView)]
    public async Task<IActionResult> GetReceipt(long id, [FromQuery] long? paymentId, CancellationToken ct)
    {
        var pdf = await billing.GenerateReceiptAsync(id, paymentId, ct);
        return File(pdf, "application/pdf", $"bill-receipt-{id}.pdf");
    }

    [HttpPost("{id:long}/advances/draw")]
    [HasPermission(PermissionRegistry.AdvanceDraw)]
    public async Task<ActionResult<ApiResponse>> DrawAdvance(long id, DrawAdvanceOnBillRequest request, CancellationToken ct)
    {
        await billing.DrawAdvanceAsync(id, request, ct);
        return OkResponse<object?>(null, "Advance drawn against this bill.");
    }

    [HttpGet("{id:long}/notes")]
    [HasPermission(PermissionRegistry.BillView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BillNoteDto>>>> GetNotes(long id, CancellationToken ct) =>
        OkResponse(await billing.GetNotesAsync(id, ct));

    [HttpPost("{id:long}/notes")]
    [HasPermission(PermissionRegistry.BillNoteManage)]
    public async Task<ActionResult<ApiResponse>> AddNote(long id, AddBillNoteRequest request, CancellationToken ct)
    {
        await billing.AddNoteAsync(id, request, ct);
        return OkResponse<object?>(null, "Note added.");
    }

    [HttpPost("{id:long}/park")]
    [HasPermission(PermissionRegistry.BillPark)]
    public async Task<ActionResult<ApiResponse>> Park(long id, ParkBillRequest request, CancellationToken ct)
    {
        await billing.ParkAsync(id, request, ct);
        return OkResponse<object?>(null, "Bill parked.");
    }

    [HttpPost("{id:long}/unpark")]
    [HasPermission(PermissionRegistry.BillParkApprove)]
    public async Task<ActionResult<ApiResponse>> Unpark(long id, CancellationToken ct)
    {
        await billing.UnparkAsync(id, ct);
        return OkResponse<object?>(null, "Bill unparked.");
    }

    [HttpPost("{id:long}/credit-notes")]
    [HasPermission(PermissionRegistry.CreditNoteCreate)]
    public async Task<ActionResult<ApiResponse<CreditNoteDto>>> RequestCreditNote(long id, RequestCreditNoteRequest request, CancellationToken ct)
    {
        var creditNote = await billing.RequestCreditNoteAsync(id, request, ct);
        return CreatedResponse($"/api/v1/billing/credit-notes/{creditNote.Id}", creditNote, "Credit note requested.");
    }
}
