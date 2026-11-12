using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Billing;
using Prixo.Domain.Entities.Ipd;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 6 billing tables. Schema: billing. PaymentMode is global; everything else is
// branch-scoped (Bill/Advance/DayClose directly, their children through the parent).

public class PaymentModeConfiguration : IEntityTypeConfiguration<PaymentMode>
{
    public void Configure(EntityTypeBuilder<PaymentMode> builder)
    {
        builder.ToTable("payment_modes", "billing");
        builder.Property(m => m.Code).HasMaxLength(30);
        builder.Property(m => m.Name).HasMaxLength(100);
        builder.Property(m => m.Description).HasMaxLength(300);
        builder.HasIndex(m => m.Code).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(m => m.LinkedAccount).WithMany().HasForeignKey(m => m.LinkedAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.ToTable("bills", "billing");
        builder.Property(b => b.BillNumber).HasMaxLength(30);
        builder.Property(b => b.SubTotal).HasPrecision(18, 2);
        builder.Property(b => b.BillDiscountPercent).HasPrecision(5, 2);
        builder.Property(b => b.DiscountAmount).HasPrecision(18, 2);
        builder.Property(b => b.TaxAmount).HasPrecision(18, 2);
        builder.Property(b => b.TotalAmount).HasPrecision(18, 2);
        builder.Property(b => b.PaidAmount).HasPrecision(18, 2);
        builder.Property(b => b.BalanceAmount).HasPrecision(18, 2);
        builder.Property(b => b.CancelReason).HasMaxLength(500);

        builder.HasIndex(b => new { b.BranchId, b.BillNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(b => new { b.BranchId, b.BillDate, b.Status });
        builder.HasIndex(b => b.PatientId);
        builder.HasIndex(b => b.OpVisitId).IsUnique().HasFilter("is_deleted = false AND op_visit_id IS NOT NULL");
        builder.HasIndex(b => b.AdmissionId).IsUnique().HasFilter("is_deleted = false AND admission_id IS NOT NULL AND status <> 4");
        builder.HasIndex(b => b.EmergencyVisitId).IsUnique().HasFilter("is_deleted = false AND emergency_visit_id IS NOT NULL AND status <> 4");

        builder.HasOne(b => b.Patient).WithMany()
            .HasForeignKey(b => b.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.OpVisit).WithMany()
            .HasForeignKey(b => b.OpVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Admission).WithMany()
            .HasForeignKey(b => b.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.EmergencyVisit).WithMany()
            .HasForeignKey(b => b.EmergencyVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Corporate).WithMany()
            .HasForeignKey(b => b.CorporateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.InsuranceCompany).WithMany()
            .HasForeignKey(b => b.InsuranceCompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(b => b.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BillItemConfiguration : IEntityTypeConfiguration<BillItem>
{
    public void Configure(EntityTypeBuilder<BillItem> builder)
    {
        builder.ToTable("bill_items", "billing");
        builder.Property(i => i.Description).HasMaxLength(300);
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.Rate).HasPrecision(18, 2);
        builder.Property(i => i.DiscountPercent).HasPrecision(5, 2);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Property(i => i.TaxPercent).HasPrecision(5, 2);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);

        builder.HasIndex(i => i.BillId);
        builder.HasIndex(i => i.ClinicalOrderId).HasFilter("is_deleted = false AND clinical_order_id IS NOT NULL");

        builder.HasOne(i => i.Bill).WithMany(b => b.Items)
            .HasForeignKey(i => i.BillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Service).WithMany()
            .HasForeignKey(i => i.ServiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Package).WithMany()
            .HasForeignKey(i => i.PackageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.ClinicalOrder).WithMany()
            .HasForeignKey(i => i.ClinicalOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BillPaymentConfiguration : IEntityTypeConfiguration<BillPayment>
{
    public void Configure(EntityTypeBuilder<BillPayment> builder)
    {
        builder.ToTable("bill_payments", "billing");
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.ReferenceNumber).HasMaxLength(100);
        builder.Property(p => p.ReceiptNumber).HasMaxLength(30);

        builder.HasIndex(p => p.ReceiptNumber).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(p => p.BillId);
        builder.HasIndex(p => p.CashierShiftId);

        builder.HasOne(p => p.Bill).WithMany(b => b.Payments)
            .HasForeignKey(p => p.BillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.PaymentMode).WithMany()
            .HasForeignKey(p => p.PaymentModeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.CashierShift).WithMany()
            .HasForeignKey(p => p.CashierShiftId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BillEstimateConfiguration : IEntityTypeConfiguration<BillEstimate>
{
    public void Configure(EntityTypeBuilder<BillEstimate> builder)
    {
        builder.ToTable("bill_estimates", "billing");
        builder.Property(e => e.EstimateNumber).HasMaxLength(30);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasIndex(e => new { e.BranchId, e.EstimateNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(e => e.PatientId);

        builder.HasOne(e => e.Patient).WithMany()
            .HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.BillEstimateTemplate).WithMany()
            .HasForeignKey(e => e.BillEstimateTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BillEstimateItemConfiguration : IEntityTypeConfiguration<BillEstimateItem>
{
    public void Configure(EntityTypeBuilder<BillEstimateItem> builder)
    {
        builder.ToTable("bill_estimate_items", "billing");
        builder.Property(i => i.Description).HasMaxLength(300);
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.Rate).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);

        builder.HasIndex(i => i.BillEstimateId);

        builder.HasOne(i => i.BillEstimate).WithMany(e => e.Items)
            .HasForeignKey(i => i.BillEstimateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Service).WithMany()
            .HasForeignKey(i => i.ServiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Package).WithMany()
            .HasForeignKey(i => i.PackageId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CashierShiftConfiguration : IEntityTypeConfiguration<CashierShift>
{
    public void Configure(EntityTypeBuilder<CashierShift> builder)
    {
        builder.ToTable("cashier_shifts", "billing");
        builder.Property(s => s.OpeningFloat).HasPrecision(18, 2);
        builder.Property(s => s.Notes).HasMaxLength(500);

        builder.HasIndex(s => new { s.BranchId, s.UserId, s.Status });
        // Postgres partial unique index: a user can have at most one Open shift at a time.
        builder.HasIndex(s => s.UserId).IsUnique().HasFilter("is_deleted = false AND status = 1");

        builder.HasOne(s => s.User).WithMany()
            .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.ReceivedByUser).WithMany()
            .HasForeignKey(s => s.ReceivedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CashierShiftModeAmountConfiguration : IEntityTypeConfiguration<CashierShiftModeAmount>
{
    public void Configure(EntityTypeBuilder<CashierShiftModeAmount> builder)
    {
        builder.ToTable("cashier_shift_mode_amounts", "billing");
        builder.Property(m => m.SystemAmount).HasPrecision(18, 2);
        builder.Property(m => m.DeclaredAmount).HasPrecision(18, 2);
        builder.Property(m => m.Variance).HasPrecision(18, 2);

        builder.HasIndex(m => new { m.CashierShiftId, m.PaymentModeId }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(m => m.CashierShift).WithMany(s => s.ModeAmounts)
            .HasForeignKey(m => m.CashierShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.PaymentMode).WithMany()
            .HasForeignKey(m => m.PaymentModeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds", "billing");
        builder.Property(r => r.Amount).HasPrecision(18, 2);
        builder.Property(r => r.Reason).HasMaxLength(500);
        builder.Property(r => r.RejectionReason).HasMaxLength(500);

        builder.HasIndex(r => r.BillId);
        builder.HasIndex(r => r.AdvanceId);
        builder.HasIndex(r => r.Status);

        builder.HasOne(r => r.Bill).WithMany()
            .HasForeignKey(r => r.BillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.BillPayment).WithMany()
            .HasForeignKey(r => r.BillPaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Advance).WithMany()
            .HasForeignKey(r => r.AdvanceId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("ck_refunds_bill_or_advance",
            "(bill_id IS NOT NULL AND advance_id IS NULL) OR (bill_id IS NULL AND advance_id IS NOT NULL)"));
    }
}

public class DiscountApprovalConfiguration : IEntityTypeConfiguration<DiscountApproval>
{
    public void Configure(EntityTypeBuilder<DiscountApproval> builder)
    {
        builder.ToTable("discount_approvals", "billing");
        builder.Property(a => a.RequestedPercent).HasPrecision(5, 2);
        builder.Property(a => a.RequestedAmount).HasPrecision(18, 2);
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.RejectionReason).HasMaxLength(500);

        builder.HasIndex(a => a.BillId);
        builder.HasIndex(a => a.Status);

        builder.HasOne(a => a.Bill).WithMany()
            .HasForeignKey(a => a.BillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.BillItem).WithMany()
            .HasForeignKey(a => a.BillItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.DiscountType).WithMany()
            .HasForeignKey(a => a.DiscountTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AdvanceConfiguration : IEntityTypeConfiguration<Advance>
{
    public void Configure(EntityTypeBuilder<Advance> builder)
    {
        builder.ToTable("advances", "billing");
        builder.Property(a => a.Amount).HasPrecision(18, 2);
        builder.Property(a => a.BalanceAmount).HasPrecision(18, 2);
        builder.Property(a => a.ReferenceNumber).HasMaxLength(100);
        builder.Property(a => a.Notes).HasMaxLength(300);

        builder.HasIndex(a => a.PatientId);
        builder.HasIndex(a => a.AdmissionId);
        builder.HasIndex(a => new { a.BranchId, a.ReceivedAt });

        builder.HasOne(a => a.Patient).WithMany()
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Admission).WithMany()
            .HasForeignKey(a => a.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.PaymentMode).WithMany()
            .HasForeignKey(a => a.PaymentModeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DayCloseConfiguration : IEntityTypeConfiguration<DayClose>
{
    public void Configure(EntityTypeBuilder<DayClose> builder)
    {
        builder.ToTable("day_closes", "billing");
        builder.Property(d => d.Notes).HasMaxLength(500);

        builder.HasIndex(d => new { d.BranchId, d.Date }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(d => d.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DayCloseModeAmountConfiguration : IEntityTypeConfiguration<DayCloseModeAmount>
{
    public void Configure(EntityTypeBuilder<DayCloseModeAmount> builder)
    {
        builder.ToTable("day_close_mode_amounts", "billing");
        builder.Property(m => m.SystemAmount).HasPrecision(18, 2);
        builder.Property(m => m.DeclaredAmount).HasPrecision(18, 2);
        builder.Property(m => m.Variance).HasPrecision(18, 2);

        builder.HasIndex(m => new { m.DayCloseId, m.PaymentModeId }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(m => m.DayClose).WithMany(d => d.ModeAmounts)
            .HasForeignKey(m => m.DayCloseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.PaymentMode).WithMany()
            .HasForeignKey(m => m.PaymentModeId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Payment gateway outbox placeholder (plan §21) — see OnlinePaymentRequest's doc comment. Global table, keyed off Bill/Patient rather than branch-scoped, matching NotificationQueue's posture.</summary>
public class OnlinePaymentRequestConfiguration : IEntityTypeConfiguration<OnlinePaymentRequest>
{
    public void Configure(EntityTypeBuilder<OnlinePaymentRequest> builder)
    {
        builder.ToTable("online_payment_requests", "billing");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Amount).HasPrecision(18, 2);
        builder.HasIndex(r => r.BillId);
        builder.HasIndex(r => r.PatientId);

        builder.HasOne(r => r.Bill).WithMany().HasForeignKey(r => r.BillId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BillNoteConfiguration : IEntityTypeConfiguration<BillNote>
{
    public void Configure(EntityTypeBuilder<BillNote> builder)
    {
        builder.ToTable("bill_notes", "billing");
        builder.Property(n => n.Note).HasMaxLength(1000);

        builder.HasIndex(n => n.BillId);

        builder.HasOne(n => n.Bill).WithMany()
            .HasForeignKey(n => n.BillId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BillCreditNoteConfiguration : IEntityTypeConfiguration<BillCreditNote>
{
    public void Configure(EntityTypeBuilder<BillCreditNote> builder)
    {
        builder.ToTable("bill_credit_notes", "billing");
        builder.Property(c => c.CreditNoteNumber).HasMaxLength(30);
        builder.Property(c => c.Reason).HasMaxLength(500);
        builder.Property(c => c.Amount).HasPrecision(18, 2);
        builder.Property(c => c.RejectionReason).HasMaxLength(500);

        builder.HasIndex(c => c.CreditNoteNumber).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(c => c.BillId);
        builder.HasIndex(c => c.Status);

        builder.HasOne(c => c.Bill).WithMany()
            .HasForeignKey(c => c.BillId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BillCreditNoteItemConfiguration : IEntityTypeConfiguration<BillCreditNoteItem>
{
    public void Configure(EntityTypeBuilder<BillCreditNoteItem> builder)
    {
        builder.ToTable("bill_credit_note_items", "billing");
        builder.Property(i => i.Amount).HasPrecision(18, 2);

        builder.HasIndex(i => i.BillCreditNoteId);

        builder.HasOne(i => i.BillCreditNote).WithMany(c => c.Items)
            .HasForeignKey(i => i.BillCreditNoteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.BillItem).WithMany()
            .HasForeignKey(i => i.BillItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
