using Prixo.Domain.Common;
using Prixo.Domain.Entities.Er;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Billing;

/// <summary>
/// Bill header. IP bills (BillType.Ip, AdmissionId set — Phase 9) get their BedCharge lines
/// auto-managed by BillingService.RefreshBedChargesAsync; everything else (Service/Package
/// lines, discounts, payments, cancellation) reuses the exact same OP machinery. There's
/// still no daily Hangfire job auto-refreshing charges or auto-feeding Pharmacy/OT — that
/// stays a manual "Refresh" action, an honest scoping call matching Pharmacy's alerts.
/// Emergency bills (BillType.Emergency, EmergencyVisitId set — Phase 16) are generated empty
/// ("post-facto friendly" per the plan) and billed entirely through the manual-item path,
/// same as an OT charge. CorporateId/InsuranceCompanyId are placeholders (plan §6, §21 —
/// held modules).
/// </summary>
public class Bill : BranchEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    /// <summary>Set when the bill was generated from an OPD visit (the common OP path).</summary>
    public long? OpVisitId { get; set; }
    public OpVisit? OpVisit { get; set; }
    /// <summary>Set when the bill is an IP bill generated against an admission (Phase 9).</summary>
    public long? AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    /// <summary>Set when the bill is generated against an ER visit (Phase 16).</summary>
    public long? EmergencyVisitId { get; set; }
    public EmergencyVisit? EmergencyVisit { get; set; }

    public required string BillNumber { get; set; }
    public DateOnly BillDate { get; set; }
    public BillType BillType { get; set; } = BillType.Op;
    public BillStatus Status { get; set; } = BillStatus.Open;

    public decimal SubTotal { get; set; }
    /// <summary>Approved bill-level discount percent, applied on top of any per-item discounts.</summary>
    public decimal BillDiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }

    /// <summary>Payment deferred to a corporate/credit account rather than collected at the counter.</summary>
    public bool IsCreditBill { get; set; }
    public long? CorporateId { get; set; }
    public Corporate? Corporate { get; set; }
    /// <summary>Placeholder — Insurance/TPA is a held module (plan §21).</summary>
    public long? InsuranceCompanyId { get; set; }
    public InsuranceCompany? InsuranceCompany { get; set; }

    public string? CancelReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public ICollection<BillItem> Items { get; set; } = [];
    public ICollection<BillPayment> Payments { get; set; } = [];
}
