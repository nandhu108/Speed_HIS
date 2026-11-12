using Prixo.Application.Common.Models;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Billing;

public record BillItemDto(
    long Id, BillItemSourceType SourceType, long? ServiceId, string? ServiceName, long? PackageId,
    string? PackageName, long? ClinicalOrderId, string Description, decimal Quantity, decimal Rate,
    decimal DiscountPercent, decimal DiscountAmount, decimal TaxPercent, decimal TaxAmount, decimal TotalAmount);

public record BillPaymentDto(
    long Id, long PaymentModeId, string PaymentModeName, decimal Amount, string? ReferenceNumber,
    string ReceiptNumber, DateTimeOffset PaidAt);

public record BillListItemDto(
    long Id, string BillNumber, DateOnly BillDate, long PatientId, string PatientUhid, string PatientName,
    BillType BillType, BillStatus Status, decimal TotalAmount, decimal PaidAmount, decimal BalanceAmount,
    bool IsCreditBill, long? AdmissionId);

/// <summary>
/// Set only when this bill's encounter (by OpVisitId/AdmissionId) has a not-yet-resolved
/// insurance pre-auth, or this exact bill has a not-yet-resolved claim (a Claim outranks a
/// PreAuthRequest when both exist, since it's the later stage) — see "Insurance awareness on
/// the bill" in docs/modules/billing.md. Advisory only, same "banner not a gate" posture as
/// CreditLimit/CreditExposure below: nothing here blocks cash collection.
/// </summary>
public record BillInsuranceStatusDto(
    long? PreAuthRequestId, string? PreAuthNumber, PreAuthStatus? PreAuthStatus, decimal? PreAuthApprovedAmount,
    long? ClaimId, string? ClaimNumber, ClaimStatus? ClaimStatus);

/// <summary>CreditLimit/CreditExposure are set only for an IP bill on an admission with a CreditLimit configured — CreditExposure = BalanceAmount minus available advances.</summary>
public record BillDetailDto(
    long Id, string BillNumber, DateOnly BillDate, long PatientId, string PatientUhid, string PatientName,
    string PatientMobile, long? OpVisitId, long? AdmissionId, long? EmergencyVisitId, BillType BillType, BillStatus Status, decimal SubTotal,
    decimal DiscountAmount, decimal TaxAmount, decimal TotalAmount, decimal PaidAmount, decimal BalanceAmount,
    bool IsCreditBill, long? CorporateId, string? CorporateName, string? CancelReason, DateTimeOffset? CancelledAt,
    decimal? CreditLimit, decimal? CreditExposure, BillInsuranceStatusDto? InsuranceStatus,
    IReadOnlyList<BillItemDto> Items, IReadOnlyList<BillPaymentDto> Payments);

// ── Bill notes ───────────────────────────────────────────────────────────
public record BillNoteDto(long Id, string Note, long CreatedBy, string? CreatedByName, DateTimeOffset CreatedAt);

public record AddBillNoteRequest(string Note);

// ── Bill parking ─────────────────────────────────────────────────────────
public record ParkBillRequest(string Reason);

// ── Advance drawdown ─────────────────────────────────────────────────────
public record DrawAdvanceOnBillRequest(long AdvanceId, decimal Amount);

// ── Bill credit notes ────────────────────────────────────────────────────
public record CreditNoteItemDto(long Id, long BillItemId, string BillItemDescription, decimal Amount);

public record CreditNoteDto(
    long Id, long BillId, string BillNumber, string CreditNoteNumber, DateOnly CreditNoteDate, string Reason,
    decimal Amount, CreditNoteStatus Status, long? DecidedByUserId, string? DecidedByName, DateTimeOffset? DecidedAt,
    string? RejectionReason, DateTimeOffset CreatedAt, IReadOnlyList<CreditNoteItemDto> Items);

public record CreditNoteItemRequest(long BillItemId, decimal Amount);

public record RequestCreditNoteRequest(string Reason, IReadOnlyList<CreditNoteItemRequest> Items);

public record DecideCreditNoteRequest(bool Approve, string? RejectionReason);

public record GenerateOpBillRequest(long OpVisitId);

public record AddManualBillItemRequest(BillItemSourceType SourceType, long? ServiceId, long? PackageId, decimal Quantity);

public record UpdateBillItemRequest(string Description, decimal Quantity, decimal Rate);

public record ApplyItemDiscountRequest(long? DiscountTypeId, decimal Percent, string? Reason);

public record ApplyBillDiscountRequest(long? DiscountTypeId, decimal Percent, string? Reason);

public record CollectPaymentRequest(long PaymentModeId, decimal Amount, string? ReferenceNumber);

public record CancelBillRequest(string Reason);

public record MakeCreditBillRequest(long? CorporateId);

public class BillQuery : PagedQuery
{
    public long? PatientId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public BillStatus? Status { get; set; }
    public BillType? BillType { get; set; }
    public long? CorporateId { get; set; }
    /// <summary>The user who generated the bill (legacy "Billed User" filter) — Bill.CreatedBy.</summary>
    public long? BilledByUserId { get; set; }
}

/// <summary>
/// OP Billing's own patient search (legacy "OP Bill Payment" patient search) — distinct from
/// Patient Registration/Front Office's patient search: this one lists *today's OP visits*
/// (unbilled or billed) for a cashier to click into and generate/open that visit's bill, not
/// patient master records. RegNo/Mobile/Gender/PatientName filter the visit's Patient; IpNumber
/// matches any Admission.AdmissionNumber for that patient (a patient can be an OP walk-in and
/// have a past/current IP admission). MrdOnly/EmrOnly/PackageOnly mirror the legacy screen's
/// three checkboxes: MrdOnly narrows to patients with an MRD RecordFile (Mrd module is
/// admission-scoped, so this only ever matches previously-admitted patients); EmrOnly narrows to
/// patients with at least one EMR ClinicalNote; PackageOnly narrows to Category == Mhc visits
/// (the "OP Multiple Visit Package" checkbox — an MHC checkup package, the only real
/// multi-visit-package concept this codebase has).
/// </summary>
public class OpBillingSearchQuery : PagedQuery
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
    public string? RegNo { get; set; }
    public string? Mobile { get; set; }
    public Gender? Gender { get; set; }
    public string? IpNumber { get; set; }
    public bool MrdOnly { get; set; }
    public bool EmrOnly { get; set; }
    public bool PackageOnly { get; set; }
}

/// <summary>BillId/BillNumber/Status are null when the visit has no non-cancelled bill yet — the
/// frontend uses that to show "Generate" vs. "Open" on the row.</summary>
public record OpBillingSearchResultDto(
    long OpVisitId, long PatientId, string RegNo, string PatientName, DateOnly EncounterDate,
    string Age, Gender Gender, string Mobile, string? DoctorName, string? SpecialtyName,
    string? PatientType, string? SourceName, long? BillId, string? BillNumber, BillStatus? Status,
    string? CorporateName);

// ── Refunds ──────────────────────────────────────────────────────────────
/// <summary>Exactly one of (BillId, BillNumber) / AdvanceId is populated — a bill refund or an advance refund. IssuedByUserId/IssuedAt are set once the money has actually moved (see RefundStatus.Issued), separate from the ApprovedByUserId/ApprovedAt decision.</summary>
public record RefundDto(
    long Id, long? BillId, string? BillNumber, long? BillPaymentId, long? AdvanceId, decimal Amount, string Reason,
    RefundMethod Method, RefundStatus Status, long? ApprovedByUserId, string? ApprovedByName, DateTimeOffset? ApprovedAt,
    long? IssuedByUserId, string? IssuedByName, DateTimeOffset? IssuedAt, string? RejectionReason, DateTimeOffset CreatedAt);

public record RequestRefundRequest(long? BillPaymentId, decimal Amount, string Reason, RefundMethod Method);

/// <summary>Requests a refund against an advance's unused BalanceAmount — no BillPaymentId concept, there's no payment row to pick from.</summary>
public record RequestAdvanceRefundRequest(decimal Amount, string Reason, RefundMethod Method);

public record DecideRefundRequest(bool Approve, string? RejectionReason);

// ── Discount approvals ───────────────────────────────────────────────────
public record DiscountApprovalDto(
    long Id, long BillId, string BillNumber, long? BillItemId, string? BillItemDescription,
    decimal RequestedPercent, decimal RequestedAmount, string? Reason, DiscountApprovalStatus Status,
    long? DecidedByUserId, string? DecidedByName, DateTimeOffset? DecidedAt, string? RejectionReason,
    DateTimeOffset CreatedAt);

public record DecideDiscountRequest(bool Approve, string? RejectionReason);
