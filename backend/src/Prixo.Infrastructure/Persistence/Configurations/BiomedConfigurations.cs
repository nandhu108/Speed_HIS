using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Biomed;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 19 Biomedical Engineering tables. Schema: biomed. Money numeric(18,2), quantities
// numeric(18,3) per CLAUDE.md.

public class BiomedEquipmentConfiguration : IEntityTypeConfiguration<BiomedEquipment>
{
    public void Configure(EntityTypeBuilder<BiomedEquipment> builder)
    {
        builder.ToTable("equipment", "biomed");
        builder.Property(e => e.EquipmentCode).HasMaxLength(30);
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.SerialNumber).HasMaxLength(100);
        builder.Property(e => e.Model).HasMaxLength(100);
        builder.Property(e => e.Manufacturer).HasMaxLength(150);
        builder.Property(e => e.CurrentLocation).HasMaxLength(200);
        builder.Property(e => e.PurchaseCost).HasPrecision(18, 2);
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.HasIndex(e => e.EquipmentCode).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Vendor).WithMany().HasForeignKey(e => e.VendorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EquipmentLocationLogConfiguration : IEntityTypeConfiguration<EquipmentLocationLog>
{
    public void Configure(EntityTypeBuilder<EquipmentLocationLog> builder)
    {
        builder.ToTable("equipment_location_logs", "biomed");
        builder.Property(l => l.FromLocation).HasMaxLength(200);
        builder.Property(l => l.ToLocation).HasMaxLength(200);
        builder.Property(l => l.Reason).HasMaxLength(500);
        builder.HasIndex(l => new { l.EquipmentId, l.MovedAt });

        builder.HasOne(l => l.Equipment).WithMany().HasForeignKey(l => l.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.MovedByUser).WithMany().HasForeignKey(l => l.MovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AmcContractConfiguration : IEntityTypeConfiguration<AmcContract>
{
    public void Configure(EntityTypeBuilder<AmcContract> builder)
    {
        builder.ToTable("amc_contracts", "biomed");
        builder.Property(c => c.ContractNumber).HasMaxLength(50);
        builder.Property(c => c.Cost).HasPrecision(18, 2);
        builder.Property(c => c.Notes).HasMaxLength(500);
        builder.HasIndex(c => c.EquipmentId);

        builder.HasOne(c => c.Equipment).WithMany().HasForeignKey(c => c.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Vendor).WithMany().HasForeignKey(c => c.VendorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PmScheduleConfiguration : IEntityTypeConfiguration<PmSchedule>
{
    public void Configure(EntityTypeBuilder<PmSchedule> builder)
    {
        builder.ToTable("pm_schedules", "biomed");
        builder.Property(p => p.ChecklistNotes).HasMaxLength(1000);
        builder.HasIndex(p => new { p.EquipmentId, p.NextDueDate });

        builder.HasOne(p => p.Equipment).WithMany().HasForeignKey(p => p.EquipmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("work_orders", "biomed");
        builder.Property(w => w.WorkOrderNumber).HasMaxLength(30);
        builder.Property(w => w.Notes).HasMaxLength(1000);
        builder.HasIndex(w => new { w.BranchId, w.WorkOrderNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(w => new { w.EquipmentId, w.Status });

        builder.HasOne(w => w.Equipment).WithMany().HasForeignKey(w => w.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(w => w.PmSchedule).WithMany().HasForeignKey(w => w.PmScheduleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(w => w.BreakdownComplaint).WithMany().HasForeignKey(w => w.BreakdownComplaintId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(w => w.AssignedToUser).WithMany().HasForeignKey(w => w.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BreakdownComplaintConfiguration : IEntityTypeConfiguration<BreakdownComplaint>
{
    public void Configure(EntityTypeBuilder<BreakdownComplaint> builder)
    {
        builder.ToTable("breakdown_complaints", "biomed");
        builder.Property(c => c.ComplaintNumber).HasMaxLength(30);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.Resolution).HasMaxLength(1000);
        builder.HasIndex(c => new { c.BranchId, c.ComplaintNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(c => new { c.EquipmentId, c.Status });

        builder.HasOne(c => c.Equipment).WithMany().HasForeignKey(c => c.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.ReportedByUser).WithMany().HasForeignKey(c => c.ReportedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.AssignedToUser).WithMany().HasForeignKey(c => c.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CalibrationConfiguration : IEntityTypeConfiguration<Calibration>
{
    public void Configure(EntityTypeBuilder<Calibration> builder)
    {
        builder.ToTable("calibrations", "biomed");
        builder.Property(c => c.PerformedBy).HasMaxLength(150);
        builder.Property(c => c.CertificateNumber).HasMaxLength(100);
        builder.Property(c => c.CertificateFilePath).HasMaxLength(500);
        builder.Property(c => c.Notes).HasMaxLength(500);
        builder.HasIndex(c => new { c.EquipmentId, c.NextDueDate });

        builder.HasOne(c => c.Equipment).WithMany().HasForeignKey(c => c.EquipmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ServiceHistoryEntryConfiguration : IEntityTypeConfiguration<ServiceHistoryEntry>
{
    public void Configure(EntityTypeBuilder<ServiceHistoryEntry> builder)
    {
        builder.ToTable("service_histories", "biomed");
        builder.Property(s => s.ServiceType).HasMaxLength(100);
        builder.Property(s => s.PerformedBy).HasMaxLength(150);
        builder.Property(s => s.Cost).HasPrecision(18, 2);
        builder.Property(s => s.Notes).HasMaxLength(500);
        builder.HasIndex(s => new { s.EquipmentId, s.ServiceDate });

        builder.HasOne(s => s.Equipment).WithMany().HasForeignKey(s => s.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.WorkOrder).WithMany().HasForeignKey(s => s.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SpareUsageConfiguration : IEntityTypeConfiguration<SpareUsage>
{
    public void Configure(EntityTypeBuilder<SpareUsage> builder)
    {
        builder.ToTable("spare_usages", "biomed");
        builder.Property(s => s.Quantity).HasPrecision(18, 3);
        builder.HasIndex(s => s.WorkOrderId);

        builder.HasOne(s => s.WorkOrder).WithMany(w => w.SpareUsages).HasForeignKey(s => s.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Batch).WithMany().HasForeignKey(s => s.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}
