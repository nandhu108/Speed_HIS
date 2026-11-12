using Prixo.Application.Common.Models;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Billing;

public class BillingReportDateRange
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

// ── Ageing / Ward-wise Outstanding ──────────────────────────────────────────
public record AgeingBillRowDto(
    long BillId, string BillNumber, DateOnly BillDate, string PatientName, decimal BalanceAmount, int DaysOutstanding, string Bucket);
public record AgeingReportDto(IReadOnlyList<AgeingBillRowDto> Bills, decimal TotalOutstanding);

public record WardOutstandingRowDto(string WardName, decimal BalanceAmount, int BillCount);
public record WardWiseOutstandingReportDto(IReadOnlyList<WardOutstandingRowDto> Wards, decimal TotalOutstanding);

/// <summary>
/// Per-admission detail, Report Type "detail" on Ward-wise Outstanding — RoomChargeAmount/
/// ServiceAmount is a real BillItem.SourceType split (BedCharge vs everything else), not the
/// legacy screen's Investigation/Pharmacy/Other-Service breakdown: this schema has no seeded
/// ServiceCategory taxonomy to derive "Investigation" from, and IP pharmacy issues never post
/// to the Bill at all (they only touch Pharmacy's own stock ledger), so a 3-way split would
/// show numbers that don't mean what they claim. AdvanceAmount is the admission's own unapplied
/// advance balance (Advance.AdmissionId), not the patient's advances on other admissions.
/// </summary>
public record WardOutstandingDetailRowDto(
    string RegNo, string Age, string IpNumber, string PatientName, DateTimeOffset AdmissionDate, string BedCode,
    string WardName, string? DepartmentName, decimal RoomChargeAmount, decimal ServiceAmount, decimal BillAmount,
    decimal AdvanceAmount, decimal BalanceAmount, bool IsCredit, string? CorporateName);
public record WardOutstandingDetailReportDto(IReadOnlyList<WardOutstandingDetailRowDto> Rows, decimal TotalBalance);

// ── Day Book ─────────────────────────────────────────────────────────────
public record DayBookBillRowDto(string BillNumber, string PatientName, decimal TotalAmount);
public record DayBookPaymentRowDto(string ReceiptNumber, string BillNumber, string PatientName, string PaymentModeName, decimal Amount, DateTimeOffset PaidAt);
public record DayBookReportDto(DateOnly Date, IReadOnlyList<DayBookBillRowDto> BillsGenerated, IReadOnlyList<DayBookPaymentRowDto> PaymentsCollected, decimal TotalBilled, decimal TotalCollected);

// ── Bill Cancelled ───────────────────────────────────────────────────────
public record CancelledBillRowDto(long BillId, string BillNumber, DateOnly BillDate, string PatientName, decimal TotalAmount, string? CancelReason, DateTimeOffset? CancelledAt);
public record CancelledBillReportDto(IReadOnlyList<CancelledBillRowDto> Bills);

// ── Concession (approved discounts) ─────────────────────────────────────
/// <summary>Search filters for the "OP Bill Concession Search" screen (legacy: OT No = the OPD visit's daily token number, OpVisit.TokenNumber).</summary>
public class ConcessionSearchQuery : PagedQuery
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? RegNo { get; set; }
    public string? PatientName { get; set; }
    public int? TokenNo { get; set; }
    public string? BillNo { get; set; }
}

/// <summary>
/// One approved-concession row with the bill/patient/doctor context the legacy search grid
/// shows. RequestedByName/RequestedAt come from DiscountApproval's own CreatedBy/CreatedAt
/// (there's no separate "requested by" field — the audit-stamp columns already mean that);
/// ApprovedByName/ApprovedAt come from DecidedByUserId/DecidedAt.
/// </summary>
public record ConcessionSearchRowDto(
    long BillId, string BillNumber, DateOnly BillDate, string RegNo, string PatientName, string Age, Gender Gender,
    string? DoctorName, string? SpecialtyName, decimal BillAmount, decimal RequestedPercent, decimal RequestedAmount,
    string? Reason, string RequestedByName, DateTimeOffset RequestedAt, string? ApprovedByName, DateTimeOffset? ApprovedAt);

// ── Refund ───────────────────────────────────────────────────────────────
/// <summary>
/// One of (BillId, BillNumber) / AdvanceId is set, never both — a bill refund or an advance
/// refund (Refund.AdvanceId). VoucherNo is the refunded BillPayment's own receipt number — a
/// refund has no generated number of its own, and "against payment" is optional
/// (Refund.BillPaymentId is nullable), so this is null for a bill-level refund not tied to one
/// specific payment, and always null for an advance refund. TotalAmount on RefundReportDto only
/// sums Issued rows (money that has actually moved), not merely Approved ones.
/// </summary>
public record RefundReportRowDto(
    long? BillId, string? BillNumber, long? AdvanceId, string RegNo, string PatientName,
    string? VoucherNo, decimal Amount, string Reason, string Status, DateTimeOffset CreatedAt);
public record RefundReportDto(IReadOnlyList<RefundReportRowDto> Refunds, decimal TotalAmount);

// ── Advance ──────────────────────────────────────────────────────────────
public record AdvanceReportRowDto(string PatientName, string PatientUhid, decimal Amount, decimal BalanceAmount, string PaymentModeName, DateTimeOffset ReceivedAt);
public record AdvanceReportDto(IReadOnlyList<AdvanceReportRowDto> Advances, decimal TotalCollected, decimal TotalBalance);

// ── Tariff List ──────────────────────────────────────────────────────────
public record TariffListRowDto(string TariffName, string? PatientCategoryName, string ServiceName, decimal Rate, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
public record TariffListReportDto(IReadOnlyList<TariffListRowDto> Rows);

// ── Doctors Payout / Performing Doctor ───────────────────────────────────
public record DoctorPayoutRowDto(long DoctorId, string DoctorName, string ServiceCategoryName, decimal BilledAmount, decimal PayoutAmount);
public record DoctorPayoutReportDto(IReadOnlyList<DoctorPayoutRowDto> Rows, decimal TotalPayout);

// ── User Settlement Denomination ─────────────────────────────────────────
public record UserSettlementRowDto(string UserName, DateTimeOffset ClosedAt, string PaymentModeName, decimal SystemAmount, decimal DeclaredAmount, decimal Variance);
public record UserSettlementReportDto(IReadOnlyList<UserSettlementRowDto> Rows);

// ── Service-wise / Header-wise Collection ────────────────────────────────
public record ServiceCategoryBreakupRowDto(string ServiceCategoryName, decimal Amount, int LineCount);
public record ServiceCategoryBreakupReportDto(IReadOnlyList<ServiceCategoryBreakupRowDto> Rows, decimal Total);

// ── IP Service Billing Detail ─────────────────────────────────────────────
public record IpServiceBillingDetailRowDto(
    string BillNumber, string PatientName, string Description, decimal Quantity, decimal Rate, decimal TotalAmount);
public record IpServiceBillingDetailReportDto(IReadOnlyList<IpServiceBillingDetailRowDto> Rows, decimal Total);

// ── Revenue Report ───────────────────────────────────────────────────────
/// <summary>
/// One row shape shared by both Report Type modes — Summary rows only populate
/// DepartmentName/BillCount/Amount (one row per department), Detail rows populate everything
/// except BillCount (one row per bill item). Left as one nullable-heavy shape rather than two
/// separate DTOs since both only ever flow straight into GetReportResultAsync's Flatten() —
/// nothing else consumes this type.
/// </summary>
public record RevenueReportRowDto(
    string? BillNumber, DateOnly? BillDate, string? PatientName, string? DoctorName, string? DepartmentName,
    string? ServiceCategoryName, string? ServiceName, int? BillCount, decimal Amount, bool? IsCredit);
public record RevenueReportDto(bool IsSummary, IReadOnlyList<RevenueReportRowDto> Rows, decimal TotalAmount);

// ── Outstanding Report ───────────────────────────────────────────────────
/// <summary>
/// General (not ward/admission-scoped, not ageing-bucketed) outstanding-balance report across
/// every bill type — Summary rows group by Bill Type (BillTypeName/BillCount/BalanceAmount
/// only); Detail rows are one row per bill. Like WardOutstandingDetailRowDto, IsCredit is the
/// sign of BalanceAmount (negative = overpaid/legacy "Credit").
/// </summary>
public record OutstandingRowDto(
    string? RegNo, string? BillNumber, DateOnly? BillDate, string? PatientName, string? BillTypeName,
    string? CorporateName, int? BillCount, decimal? TotalAmount, decimal? PaidAmount, decimal BalanceAmount, bool? IsCredit);
public record OutstandingReportDto(bool IsSummary, IReadOnlyList<OutstandingRowDto> Rows, decimal TotalBalance);
