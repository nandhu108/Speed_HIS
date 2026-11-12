using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Pharmacy;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 8 pharmacy tables. Schema: pharmacy. Store/Rack/Batch/StockLedger and every
// document header are branch-scoped directly; line-item tables inherit scoping through
// their parent. Money numeric(18,2), quantities numeric(18,3) per CLAUDE.md.

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("stores", "pharmacy");
        builder.Property(s => s.Code).HasMaxLength(30);
        builder.Property(s => s.Name).HasMaxLength(150);
        builder.HasIndex(s => new { s.BranchId, s.Code }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne<Branch>().WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RackConfiguration : IEntityTypeConfiguration<Rack>
{
    public void Configure(EntityTypeBuilder<Rack> builder)
    {
        builder.ToTable("racks", "pharmacy");
        builder.Property(r => r.Code).HasMaxLength(30);
        builder.Property(r => r.Name).HasMaxLength(150);
        builder.HasIndex(r => new { r.StoreId, r.Code }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(r => r.Store).WithMany().HasForeignKey(r => r.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BatchConfiguration : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> builder)
    {
        builder.ToTable("batches", "pharmacy");
        builder.Property(b => b.BatchNumber).HasMaxLength(50);
        builder.Property(b => b.Mrp).HasPrecision(18, 2);
        builder.Property(b => b.PurchaseRate).HasPrecision(18, 2);
        builder.Property(b => b.QuantityOnHand).HasPrecision(18, 3);

        builder.HasIndex(b => new { b.DrugId, b.StoreId, b.BatchNumber }).IsUnique().HasFilter("is_deleted = false AND drug_id IS NOT NULL");
        builder.HasIndex(b => new { b.ItemId, b.StoreId, b.BatchNumber }).IsUnique().HasFilter("is_deleted = false AND item_id IS NOT NULL");
        builder.HasIndex(b => new { b.StoreId, b.ExpiryDate });

        builder.HasOne(b => b.Drug).WithMany().HasForeignKey(b => b.DrugId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Item).WithMany().HasForeignKey(b => b.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Store).WithMany().HasForeignKey(b => b.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(b => b.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockLedgerConfiguration : IEntityTypeConfiguration<StockLedger>
{
    public void Configure(EntityTypeBuilder<StockLedger> builder)
    {
        builder.ToTable("stock_ledger", "pharmacy");
        builder.Property(l => l.Quantity).HasPrecision(18, 3);
        builder.Property(l => l.RunningBalance).HasPrecision(18, 3);
        builder.Property(l => l.ReferenceType).HasMaxLength(50);
        builder.Property(l => l.Notes).HasMaxLength(300);

        builder.HasIndex(l => new { l.BatchId, l.MovementAt });
        builder.HasIndex(l => new { l.ReferenceType, l.ReferenceId });

        builder.HasOne(l => l.Store).WithMany().HasForeignKey(l => l.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.Batch).WithMany().HasForeignKey(l => l.BatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(l => l.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        builder.ToTable("purchase_requests", "pharmacy");
        builder.Property(r => r.RequestNumber).HasMaxLength(30);
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.HasIndex(r => new { r.BranchId, r.RequestNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne<Branch>().WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseRequestItemConfiguration : IEntityTypeConfiguration<PurchaseRequestItem>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestItem> builder)
    {
        builder.ToTable("purchase_request_items", "pharmacy");
        builder.Property(i => i.RequestedQuantity).HasPrecision(18, 3);
        builder.HasIndex(i => i.PurchaseRequestId);
        builder.HasOne(i => i.PurchaseRequest).WithMany(r => r.Items).HasForeignKey(i => i.PurchaseRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Drug).WithMany().HasForeignKey(i => i.DrugId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Item).WithMany().HasForeignKey(i => i.ItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders", "pharmacy");
        builder.Property(o => o.PoNumber).HasMaxLength(30);
        builder.Property(o => o.Notes).HasMaxLength(500);
        builder.HasIndex(o => new { o.BranchId, o.PoNumber }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(o => o.Vendor).WithMany().HasForeignKey(o => o.VendorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.PurchaseRequest).WithMany().HasForeignKey(o => o.PurchaseRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(o => o.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("purchase_order_items", "pharmacy");
        builder.Property(i => i.OrderedQuantity).HasPrecision(18, 3);
        builder.Property(i => i.ReceivedQuantity).HasPrecision(18, 3);
        builder.Property(i => i.Rate).HasPrecision(18, 2);
        builder.Property(i => i.TaxPercent).HasPrecision(5, 2);
        builder.HasIndex(i => i.PurchaseOrderId);
        builder.HasOne(i => i.PurchaseOrder).WithMany(o => o.Items).HasForeignKey(i => i.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Drug).WithMany().HasForeignKey(i => i.DrugId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Item).WithMany().HasForeignKey(i => i.ItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class GrnConfiguration : IEntityTypeConfiguration<Grn>
{
    public void Configure(EntityTypeBuilder<Grn> builder)
    {
        builder.ToTable("grns", "pharmacy");
        builder.Property(g => g.GrnNumber).HasMaxLength(30);
        builder.Property(g => g.SupplierInvoiceNumber).HasMaxLength(50);
        builder.Property(g => g.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(g => new { g.BranchId, g.GrnNumber }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(g => g.PurchaseOrder).WithMany().HasForeignKey(g => g.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(g => g.Vendor).WithMany().HasForeignKey(g => g.VendorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(g => g.Store).WithMany().HasForeignKey(g => g.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(g => g.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class GrnItemConfiguration : IEntityTypeConfiguration<GrnItem>
{
    public void Configure(EntityTypeBuilder<GrnItem> builder)
    {
        builder.ToTable("grn_items", "pharmacy");
        builder.Property(i => i.BatchNumber).HasMaxLength(50);
        builder.Property(i => i.Mrp).HasPrecision(18, 2);
        builder.Property(i => i.PurchaseRate).HasPrecision(18, 2);
        builder.Property(i => i.ReceivedQuantity).HasPrecision(18, 3);
        builder.Property(i => i.FreeQuantity).HasPrecision(18, 3);
        builder.Property(i => i.TaxPercent).HasPrecision(5, 2);
        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.HasIndex(i => i.GrnId);

        builder.HasOne(i => i.Grn).WithMany(g => g.Items).HasForeignKey(i => i.GrnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Drug).WithMany().HasForeignKey(i => i.DrugId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Item).WithMany().HasForeignKey(i => i.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Batch).WithMany().HasForeignKey(i => i.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.ToTable("purchase_returns", "pharmacy");
        builder.Property(r => r.ReturnNumber).HasMaxLength(30);
        builder.Property(r => r.Reason).HasMaxLength(500);
        builder.Property(r => r.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(r => new { r.BranchId, r.ReturnNumber }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(r => r.Grn).WithMany().HasForeignKey(r => r.GrnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Vendor).WithMany().HasForeignKey(r => r.VendorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseReturnItemConfiguration : IEntityTypeConfiguration<PurchaseReturnItem>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnItem> builder)
    {
        builder.ToTable("purchase_return_items", "pharmacy");
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.Rate).HasPrecision(18, 2);
        builder.HasIndex(i => i.PurchaseReturnId);
        builder.HasOne(i => i.PurchaseReturn).WithMany(r => r.Items).HasForeignKey(i => i.PurchaseReturnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Batch).WithMany().HasForeignKey(i => i.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PharmacySaleConfiguration : IEntityTypeConfiguration<PharmacySale>
{
    public void Configure(EntityTypeBuilder<PharmacySale> builder)
    {
        builder.ToTable("pharmacy_sales", "pharmacy");
        builder.Property(s => s.SaleNumber).HasMaxLength(30);
        builder.Property(s => s.CancelReason).HasMaxLength(500);
        builder.Property(s => s.SubTotal).HasPrecision(18, 2);
        builder.Property(s => s.DiscountAmount).HasPrecision(18, 2);
        builder.Property(s => s.TaxAmount).HasPrecision(18, 2);
        builder.Property(s => s.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(s => new { s.BranchId, s.SaleNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(s => new { s.BranchId, s.SaleDate });

        builder.HasOne(s => s.Store).WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Patient).WithMany().HasForeignKey(s => s.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Prescription).WithMany().HasForeignKey(s => s.PrescriptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.PaymentMode).WithMany().HasForeignKey(s => s.PaymentModeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PharmacySaleItemConfiguration : IEntityTypeConfiguration<PharmacySaleItem>
{
    public void Configure(EntityTypeBuilder<PharmacySaleItem> builder)
    {
        builder.ToTable("pharmacy_sale_items", "pharmacy");
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.Rate).HasPrecision(18, 2);
        builder.Property(i => i.DiscountPercent).HasPrecision(5, 2);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Property(i => i.TaxPercent).HasPrecision(5, 2);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(i => i.PharmacySaleId);

        builder.HasOne(i => i.PharmacySale).WithMany(s => s.Items).HasForeignKey(i => i.PharmacySaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Batch).WithMany().HasForeignKey(i => i.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ToTable("sales_returns", "pharmacy");
        builder.Property(r => r.ReturnNumber).HasMaxLength(30);
        builder.Property(r => r.Reason).HasMaxLength(500);
        builder.Property(r => r.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(r => new { r.BranchId, r.ReturnNumber }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(r => r.PharmacySale).WithMany().HasForeignKey(r => r.PharmacySaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SalesReturnItemConfiguration : IEntityTypeConfiguration<SalesReturnItem>
{
    public void Configure(EntityTypeBuilder<SalesReturnItem> builder)
    {
        builder.ToTable("sales_return_items", "pharmacy");
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.HasIndex(i => i.SalesReturnId);

        builder.HasOne(i => i.SalesReturn).WithMany(r => r.Items).HasForeignKey(i => i.SalesReturnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.PharmacySaleItem).WithMany().HasForeignKey(i => i.PharmacySaleItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.ToTable("issues", "pharmacy");
        builder.Property(i => i.IssueNumber).HasMaxLength(30);
        builder.Property(i => i.Notes).HasMaxLength(500);
        builder.HasIndex(i => new { i.BranchId, i.IssueNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(i => new { i.BranchId, i.IssueDate, i.IssueType });

        builder.HasOne(i => i.Store).WithMany().HasForeignKey(i => i.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Department).WithMany().HasForeignKey(i => i.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Patient).WithMany().HasForeignKey(i => i.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Admission).WithMany().HasForeignKey(i => i.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Indent).WithMany().HasForeignKey(i => i.IndentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(i => i.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class IssueItemConfiguration : IEntityTypeConfiguration<IssueItem>
{
    public void Configure(EntityTypeBuilder<IssueItem> builder)
    {
        builder.ToTable("issue_items", "pharmacy");
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.HasIndex(i => i.IssueId);
        builder.HasOne(i => i.Issue).WithMany(x => x.Items).HasForeignKey(i => i.IssueId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Batch).WithMany().HasForeignKey(i => i.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class IssueReturnConfiguration : IEntityTypeConfiguration<IssueReturn>
{
    public void Configure(EntityTypeBuilder<IssueReturn> builder)
    {
        builder.ToTable("issue_returns", "pharmacy");
        builder.Property(r => r.ReturnNumber).HasMaxLength(30);
        builder.Property(r => r.Reason).HasMaxLength(500);
        builder.HasIndex(r => new { r.BranchId, r.ReturnNumber }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(r => r.Issue).WithMany().HasForeignKey(r => r.IssueId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class IssueReturnItemConfiguration : IEntityTypeConfiguration<IssueReturnItem>
{
    public void Configure(EntityTypeBuilder<IssueReturnItem> builder)
    {
        builder.ToTable("issue_return_items", "pharmacy");
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.HasIndex(i => i.IssueReturnId);

        builder.HasOne(i => i.IssueReturn).WithMany(r => r.Items).HasForeignKey(i => i.IssueReturnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.IssueItem).WithMany().HasForeignKey(i => i.IssueItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("stock_adjustments", "pharmacy");
        builder.Property(a => a.AdjustmentNumber).HasMaxLength(30);
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.RejectionReason).HasMaxLength(500);
        builder.HasIndex(a => new { a.BranchId, a.AdjustmentNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(a => a.Status);

        builder.HasOne(a => a.Store).WithMany().HasForeignKey(a => a.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockAdjustmentItemConfiguration : IEntityTypeConfiguration<StockAdjustmentItem>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentItem> builder)
    {
        builder.ToTable("stock_adjustment_items", "pharmacy");
        builder.Property(i => i.QuantityChange).HasPrecision(18, 3);
        builder.HasIndex(i => i.StockAdjustmentId);

        builder.HasOne(i => i.StockAdjustment).WithMany(a => a.Items).HasForeignKey(i => i.StockAdjustmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Batch).WithMany().HasForeignKey(i => i.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("stock_transfers", "pharmacy");
        builder.Property(t => t.TransferNumber).HasMaxLength(30);
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.HasIndex(t => new { t.BranchId, t.TransferNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(t => t.Status);

        builder.HasOne(t => t.FromStore).WithMany().HasForeignKey(t => t.FromStoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ToStore).WithMany().HasForeignKey(t => t.ToStoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ToBranch).WithMany().HasForeignKey(t => t.ToBranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(t => t.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockTransferItemConfiguration : IEntityTypeConfiguration<StockTransferItem>
{
    public void Configure(EntityTypeBuilder<StockTransferItem> builder)
    {
        builder.ToTable("stock_transfer_items", "pharmacy");
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.HasIndex(i => i.StockTransferId);

        builder.HasOne(i => i.StockTransfer).WithMany(t => t.Items).HasForeignKey(i => i.StockTransferId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Batch).WithMany().HasForeignKey(i => i.BatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.ReceivedBatch).WithMany().HasForeignKey(i => i.ReceivedBatchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockAuditConfiguration : IEntityTypeConfiguration<StockAudit>
{
    public void Configure(EntityTypeBuilder<StockAudit> builder)
    {
        builder.ToTable("stock_audits", "pharmacy");
        builder.Property(a => a.AuditNumber).HasMaxLength(30);
        builder.HasIndex(a => new { a.BranchId, a.AuditNumber }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(a => a.Store).WithMany().HasForeignKey(a => a.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockAuditItemConfiguration : IEntityTypeConfiguration<StockAuditItem>
{
    public void Configure(EntityTypeBuilder<StockAuditItem> builder)
    {
        builder.ToTable("stock_audit_items", "pharmacy");
        builder.Property(i => i.SystemQuantity).HasPrecision(18, 3);
        builder.Property(i => i.CountedQuantity).HasPrecision(18, 3);
        builder.Property(i => i.Variance).HasPrecision(18, 3);
        builder.HasIndex(i => i.StockAuditId);

        builder.HasOne(i => i.StockAudit).WithMany(a => a.Items).HasForeignKey(i => i.StockAuditId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Batch).WithMany().HasForeignKey(i => i.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}

// Phase 13: Stores & Inventory. Indent sits in front of the existing Issue engine.

public class IndentConfiguration : IEntityTypeConfiguration<Indent>
{
    public void Configure(EntityTypeBuilder<Indent> builder)
    {
        builder.ToTable("indents", "pharmacy");
        builder.Property(i => i.IndentNumber).HasMaxLength(30);
        builder.Property(i => i.Notes).HasMaxLength(500);
        builder.Property(i => i.RejectionReason).HasMaxLength(500);
        builder.HasIndex(i => new { i.BranchId, i.IndentNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(i => i.Status);

        builder.HasOne(i => i.Store).WithMany().HasForeignKey(i => i.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Department).WithMany().HasForeignKey(i => i.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.RequestedByUser).WithMany().HasForeignKey(i => i.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.DecidedByUser).WithMany().HasForeignKey(i => i.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(i => i.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class IndentItemConfiguration : IEntityTypeConfiguration<IndentItem>
{
    public void Configure(EntityTypeBuilder<IndentItem> builder)
    {
        builder.ToTable("indent_items", "pharmacy");
        builder.Property(i => i.RequestedQuantity).HasPrecision(18, 3);
        builder.Property(i => i.ApprovedQuantity).HasPrecision(18, 3);
        builder.HasIndex(i => i.IndentId);

        builder.HasOne(i => i.Indent).WithMany(x => x.Items).HasForeignKey(i => i.IndentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Drug).WithMany().HasForeignKey(i => i.DrugId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Item).WithMany().HasForeignKey(i => i.ItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
