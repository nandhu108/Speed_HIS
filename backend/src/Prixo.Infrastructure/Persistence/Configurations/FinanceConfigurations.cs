using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Finance;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 18 Finance tables. Schema: finance. Account is global (shared CoA); everything else
// is branch-scoped. Money numeric(18,2) per CLAUDE.md.

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts", "finance");
        builder.Property(a => a.Code).HasMaxLength(30);
        builder.Property(a => a.Name).HasMaxLength(200);
        builder.Property(a => a.Description).HasMaxLength(500);
        builder.HasIndex(a => a.Code).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(a => a.ParentAccount).WithMany().HasForeignKey(a => a.ParentAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.ToTable("vouchers", "finance");
        builder.Property(v => v.VoucherNumber).HasMaxLength(30);
        builder.Property(v => v.Narration).HasMaxLength(500);
        builder.Property(v => v.CancelReason).HasMaxLength(500);
        builder.Property(v => v.SourceReference).HasMaxLength(100);
        builder.HasIndex(v => new { v.BranchId, v.VoucherNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(v => new { v.BranchId, v.VoucherDate, v.VoucherType });

        builder.HasOne(v => v.PostedByUser).WithMany().HasForeignKey(v => v.PostedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.ReversalOfVoucher).WithMany().HasForeignKey(v => v.ReversalOfVoucherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(v => v.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(v => v.Lines).WithOne(l => l.Voucher).HasForeignKey(l => l.VoucherId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class VoucherLineConfiguration : IEntityTypeConfiguration<VoucherLine>
{
    public void Configure(EntityTypeBuilder<VoucherLine> builder)
    {
        builder.ToTable("voucher_lines", "finance");
        builder.Property(l => l.Narration).HasMaxLength(300);
        builder.HasIndex(l => l.VoucherId);

        builder.HasOne(l => l.Account).WithMany().HasForeignKey(l => l.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries", "finance");
        builder.Property(e => e.Narration).HasMaxLength(300);
        builder.HasIndex(e => new { e.AccountId, e.EntryDate });
        builder.HasIndex(e => e.VoucherLineId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(e => e.Voucher).WithMany().HasForeignKey(e => e.VoucherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.VoucherLine).WithMany().HasForeignKey(e => e.VoucherLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Account).WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayableConfiguration : IEntityTypeConfiguration<Payable>
{
    public void Configure(EntityTypeBuilder<Payable> builder)
    {
        builder.ToTable("payables", "finance");
        builder.Property(p => p.InvoiceNumber).HasMaxLength(100);
        builder.HasIndex(p => p.GrnId).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(p => new { p.BranchId, p.Status });

        builder.HasOne(p => p.Vendor).WithMany().HasForeignKey(p => p.VendorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Grn).WithMany().HasForeignKey(p => p.GrnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(p => p.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReceivableConfiguration : IEntityTypeConfiguration<Receivable>
{
    public void Configure(EntityTypeBuilder<Receivable> builder)
    {
        builder.ToTable("receivables", "finance");
        builder.HasIndex(r => r.BillId).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(r => new { r.BranchId, r.Status });

        builder.HasOne(r => r.Corporate).WithMany().HasForeignKey(r => r.CorporateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Bill).WithMany().HasForeignKey(r => r.BillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
