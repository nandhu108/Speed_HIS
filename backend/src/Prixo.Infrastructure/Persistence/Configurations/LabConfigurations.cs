using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Lab;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 7 lab tables. Schema: lab. Masters (LabTest/TestProfile and their children) are
// global; LabOrder/Sample/CriticalAlert are branch-scoped directly, everything else
// inherits scoping through its parent.

public class LabTestConfiguration : IEntityTypeConfiguration<LabTest>
{
    public void Configure(EntityTypeBuilder<LabTest> builder)
    {
        builder.ToTable("lab_tests", "lab");
        builder.Property(t => t.Code).HasMaxLength(30);
        builder.Property(t => t.Name).HasMaxLength(150);
        builder.Property(t => t.Method).HasMaxLength(100);

        builder.HasIndex(t => t.Code).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(t => t.Department).WithMany()
            .HasForeignKey(t => t.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.SampleType).WithMany()
            .HasForeignKey(t => t.SampleTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Service).WithMany()
            .HasForeignKey(t => t.ServiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LabTestParameterConfiguration : IEntityTypeConfiguration<LabTestParameter>
{
    public void Configure(EntityTypeBuilder<LabTestParameter> builder)
    {
        builder.ToTable("lab_test_parameters", "lab");
        builder.Property(p => p.Code).HasMaxLength(30);
        builder.Property(p => p.Name).HasMaxLength(150);
        builder.Property(p => p.Unit).HasMaxLength(30);
        builder.Property(p => p.DropdownOptions).HasMaxLength(500);

        builder.HasIndex(p => new { p.LabTestId, p.Code }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(p => p.LabTest).WithMany(t => t.Parameters)
            .HasForeignKey(p => p.LabTestId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReferenceRangeConfiguration : IEntityTypeConfiguration<ReferenceRange>
{
    public void Configure(EntityTypeBuilder<ReferenceRange> builder)
    {
        builder.ToTable("reference_ranges", "lab");
        builder.Property(r => r.NormalLow).HasPrecision(18, 3);
        builder.Property(r => r.NormalHigh).HasPrecision(18, 3);
        builder.Property(r => r.CriticalLow).HasPrecision(18, 3);
        builder.Property(r => r.CriticalHigh).HasPrecision(18, 3);
        builder.Property(r => r.TextNormalValue).HasMaxLength(200);

        builder.HasIndex(r => r.LabTestParameterId);

        builder.HasOne(r => r.LabTestParameter).WithMany(p => p.ReferenceRanges)
            .HasForeignKey(r => r.LabTestParameterId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TestProfileConfiguration : IEntityTypeConfiguration<TestProfile>
{
    public void Configure(EntityTypeBuilder<TestProfile> builder)
    {
        builder.ToTable("test_profiles", "lab");
        builder.Property(p => p.Code).HasMaxLength(30);
        builder.Property(p => p.Name).HasMaxLength(150);
        builder.Property(p => p.Price).HasPrecision(18, 2);

        builder.HasIndex(p => p.Code).IsUnique().HasFilter("is_deleted = false");
    }
}

public class ProfileTestConfiguration : IEntityTypeConfiguration<ProfileTest>
{
    public void Configure(EntityTypeBuilder<ProfileTest> builder)
    {
        builder.ToTable("profile_tests", "lab");

        builder.HasIndex(t => new { t.TestProfileId, t.LabTestId }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(t => t.TestProfile).WithMany(p => p.Tests)
            .HasForeignKey(t => t.TestProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.LabTest).WithMany()
            .HasForeignKey(t => t.LabTestId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LabOrderConfiguration : IEntityTypeConfiguration<LabOrder>
{
    public void Configure(EntityTypeBuilder<LabOrder> builder)
    {
        builder.ToTable("lab_orders", "lab");
        builder.Property(o => o.OrderNumber).HasMaxLength(30);
        builder.Property(o => o.ClinicalNotes).HasMaxLength(1000);
        builder.Property(o => o.CancelReason).HasMaxLength(500);

        builder.HasIndex(o => new { o.BranchId, o.OrderNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(o => new { o.PatientId, o.OrderDate });
        builder.HasIndex(o => new { o.BranchId, o.OrderDate, o.Status });

        builder.HasOne(o => o.Patient).WithMany()
            .HasForeignKey(o => o.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.OpVisit).WithMany()
            .HasForeignKey(o => o.OpVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.EmergencyVisit).WithMany()
            .HasForeignKey(o => o.EmergencyVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Admission).WithMany()
            .HasForeignKey(o => o.AdmissionId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.ClinicalOrder).WithMany()
            .HasForeignKey(o => o.ClinicalOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.OrderedByDoctor).WithMany()
            .HasForeignKey(o => o.OrderedByDoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(o => o.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LabOrderItemConfiguration : IEntityTypeConfiguration<LabOrderItem>
{
    public void Configure(EntityTypeBuilder<LabOrderItem> builder)
    {
        builder.ToTable("lab_order_items", "lab");

        builder.HasIndex(i => i.LabOrderId);
        builder.HasIndex(i => i.SampleId).HasFilter("is_deleted = false AND sample_id IS NOT NULL");

        builder.HasOne(i => i.LabOrder).WithMany(o => o.Items)
            .HasForeignKey(i => i.LabOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.LabTest).WithMany()
            .HasForeignKey(i => i.LabTestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.TestProfile).WithMany()
            .HasForeignKey(i => i.TestProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Sample).WithMany(s => s.Items)
            .HasForeignKey(i => i.SampleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SampleConfiguration : IEntityTypeConfiguration<Sample>
{
    public void Configure(EntityTypeBuilder<Sample> builder)
    {
        builder.ToTable("samples", "lab");
        builder.Property(s => s.SampleNumber).HasMaxLength(30);
        builder.Property(s => s.RejectionReason).HasMaxLength(500);

        builder.HasIndex(s => new { s.BranchId, s.SampleNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(s => s.LabOrderId);

        builder.HasOne(s => s.LabOrder).WithMany()
            .HasForeignKey(s => s.LabOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.SampleType).WithMany()
            .HasForeignKey(s => s.SampleTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LabResultConfiguration : IEntityTypeConfiguration<LabResult>
{
    public void Configure(EntityTypeBuilder<LabResult> builder)
    {
        builder.ToTable("lab_results", "lab");
        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder.HasIndex(r => r.LabOrderItemId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(r => r.LabOrderItem).WithOne(i => i.Result)
            .HasForeignKey<LabResult>(r => r.LabOrderItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ResultValueConfiguration : IEntityTypeConfiguration<ResultValue>
{
    public void Configure(EntityTypeBuilder<ResultValue> builder)
    {
        builder.ToTable("result_values", "lab");
        builder.Property(v => v.Value).HasMaxLength(200);
        builder.Property(v => v.Unit).HasMaxLength(30);
        builder.Property(v => v.NormalLow).HasPrecision(18, 3);
        builder.Property(v => v.NormalHigh).HasPrecision(18, 3);

        builder.HasIndex(v => v.LabResultId);

        builder.HasOne(v => v.LabResult).WithMany(r => r.Values)
            .HasForeignKey(v => v.LabResultId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.LabTestParameter).WithMany()
            .HasForeignKey(v => v.LabTestParameterId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CriticalAlertConfiguration : IEntityTypeConfiguration<CriticalAlert>
{
    public void Configure(EntityTypeBuilder<CriticalAlert> builder)
    {
        builder.ToTable("critical_alerts", "lab");
        builder.Property(a => a.AcknowledgementNotes).HasMaxLength(500);

        builder.HasIndex(a => a.LabResultId);
        builder.HasIndex(a => a.IsAcknowledged);

        builder.HasOne(a => a.LabResult).WithMany()
            .HasForeignKey(a => a.LabResultId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.ResultValue).WithMany()
            .HasForeignKey(a => a.ResultValueId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
