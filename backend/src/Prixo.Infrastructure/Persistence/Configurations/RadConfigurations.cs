using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Rad;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 12 Radiology tables. RadiologyTest lives in schema `master` (global catalog, same
// posture as Lab's LabTest); everything else is schema `rad`. RadOrder is branch-scoped
// directly, everything else inherits scoping through it — mirrors LabConfigurations exactly.

public class RadiologyTestConfiguration : IEntityTypeConfiguration<RadiologyTest>
{
    public void Configure(EntityTypeBuilder<RadiologyTest> builder)
    {
        builder.ToTable("radiology_tests", "master");
        builder.Property(t => t.Code).HasMaxLength(30);
        builder.Property(t => t.Name).HasMaxLength(150);
        builder.Property(t => t.BodyPart).HasMaxLength(100);
        builder.Property(t => t.PrepInstructions).HasMaxLength(1000);

        builder.HasIndex(t => t.Code).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(t => t.Modality).WithMany().HasForeignKey(t => t.ModalityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Service).WithMany().HasForeignKey(t => t.ServiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RadReportTemplateConfiguration : IEntityTypeConfiguration<RadReportTemplate>
{
    public void Configure(EntityTypeBuilder<RadReportTemplate> builder)
    {
        builder.ToTable("report_templates", "rad");
        builder.Property(t => t.Name).HasMaxLength(150);
        builder.Property(t => t.FindingsTemplate).HasMaxLength(4000);
        builder.Property(t => t.ImpressionTemplate).HasMaxLength(2000);

        builder.HasIndex(t => t.RadiologyTestId);

        builder.HasOne(t => t.RadiologyTest).WithMany().HasForeignKey(t => t.RadiologyTestId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RadOrderConfiguration : IEntityTypeConfiguration<RadOrder>
{
    public void Configure(EntityTypeBuilder<RadOrder> builder)
    {
        builder.ToTable("rad_orders", "rad");
        builder.Property(o => o.OrderNumber).HasMaxLength(30);
        builder.Property(o => o.ClinicalNotes).HasMaxLength(1000);
        builder.Property(o => o.CancelReason).HasMaxLength(500);

        builder.HasIndex(o => new { o.BranchId, o.OrderNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(o => new { o.PatientId, o.OrderDate });
        builder.HasIndex(o => new { o.BranchId, o.OrderDate, o.Status });

        builder.HasOne(o => o.Patient).WithMany().HasForeignKey(o => o.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.OpVisit).WithMany().HasForeignKey(o => o.OpVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Admission).WithMany().HasForeignKey(o => o.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.EmergencyVisit).WithMany().HasForeignKey(o => o.EmergencyVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.ClinicalOrder).WithMany().HasForeignKey(o => o.ClinicalOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.OrderedByDoctor).WithMany().HasForeignKey(o => o.OrderedByDoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(o => o.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RadOrderItemConfiguration : IEntityTypeConfiguration<RadOrderItem>
{
    public void Configure(EntityTypeBuilder<RadOrderItem> builder)
    {
        builder.ToTable("rad_order_items", "rad");
        builder.Property(i => i.AccessionNumber).HasMaxLength(30);
        builder.Property(i => i.PacsStudyUid).HasMaxLength(100);

        builder.HasIndex(i => i.RadOrderId);
        builder.HasIndex(i => i.AccessionNumber).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(i => i.RadOrder).WithMany(o => o.Items).HasForeignKey(i => i.RadOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.RadiologyTest).WithMany().HasForeignKey(i => i.RadiologyTestId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RadScheduleConfiguration : IEntityTypeConfiguration<RadSchedule>
{
    public void Configure(EntityTypeBuilder<RadSchedule> builder)
    {
        builder.ToTable("rad_schedules", "rad");
        builder.Property(s => s.Notes).HasMaxLength(500);

        builder.HasIndex(s => s.RadOrderItemId).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(s => new { s.ModalityId, s.ScheduledAt });

        builder.HasOne(s => s.RadOrderItem).WithOne(i => i.Schedule).HasForeignKey<RadSchedule>(s => s.RadOrderItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Modality).WithMany().HasForeignKey(s => s.ModalityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.ScheduledByUser).WithMany().HasForeignKey(s => s.ScheduledByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ContrastConsentConfiguration : IEntityTypeConfiguration<ContrastConsent>
{
    public void Configure(EntityTypeBuilder<ContrastConsent> builder)
    {
        builder.ToTable("contrast_consents", "rad");
        builder.Property(c => c.SignedByName).HasMaxLength(200);
        builder.Property(c => c.RelationshipToPatient).HasMaxLength(100);
        builder.Property(c => c.DocumentPath).HasMaxLength(500);

        builder.HasIndex(c => c.RadOrderItemId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(c => c.RadOrderItem).WithOne(i => i.ContrastConsent).HasForeignKey<ContrastConsent>(c => c.RadOrderItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RadReportConfiguration : IEntityTypeConfiguration<RadReport>
{
    public void Configure(EntityTypeBuilder<RadReport> builder)
    {
        builder.ToTable("rad_reports", "rad");
        builder.Property(r => r.Findings).HasMaxLength(4000);
        builder.Property(r => r.Impression).HasMaxLength(2000);
        builder.Property(r => r.ApproverSignaturePath).HasMaxLength(500);

        builder.HasIndex(r => r.RadOrderItemId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(r => r.RadOrderItem).WithOne(i => i.Report).HasForeignKey<RadReport>(r => r.RadOrderItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Template).WithMany().HasForeignKey(r => r.TemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.EnteredByUser).WithMany().HasForeignKey(r => r.EnteredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.VerifiedByUser).WithMany().HasForeignKey(r => r.VerifiedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.ApprovedByUser).WithMany().HasForeignKey(r => r.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
