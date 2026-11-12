using Prixo.Domain.Common;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Pharmacy;

/// <summary>
/// OP pharmacy POS sale. PrescriptionId is an optional link to an OPD prescription for
/// traceability (loose coupling, same pattern as Lab's ClinicalOrderId) — a walk-in
/// over-the-counter sale has neither a patient nor a prescription. BillItemId is a soft,
/// unenforced link to billing.bill_items, same posture as Lab's billing check.
/// </summary>
public class PharmacySale : BranchEntity
{
    public long StoreId { get; set; }
    public Store? Store { get; set; }
    public long? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long? PrescriptionId { get; set; }
    public Prescription? Prescription { get; set; }
    public required string SaleNumber { get; set; }
    public DateOnly SaleDate { get; set; }
    public PharmacySaleStatus Status { get; set; } = PharmacySaleStatus.Completed;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public long? PaymentModeId { get; set; }
    public PaymentMode? PaymentMode { get; set; }
    public long? BillItemId { get; set; }
    public string? CancelReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public ICollection<PharmacySaleItem> Items { get; set; } = [];
}

public class PharmacySaleItem : BaseEntity
{
    public long PharmacySaleId { get; set; }
    public PharmacySale? PharmacySale { get; set; }
    public long BatchId { get; set; }
    public Batch? Batch { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class SalesReturn : BranchEntity
{
    public long PharmacySaleId { get; set; }
    public PharmacySale? PharmacySale { get; set; }
    public required string ReturnNumber { get; set; }
    public DateOnly ReturnDate { get; set; }
    public required string Reason { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<SalesReturnItem> Items { get; set; } = [];
}

public class SalesReturnItem : BaseEntity
{
    public long SalesReturnId { get; set; }
    public SalesReturn? SalesReturn { get; set; }
    public long PharmacySaleItemId { get; set; }
    public PharmacySaleItem? PharmacySaleItem { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
}
