using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Opd;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 5 OPD tables. Schema: opd. Branch-scoped where noted; child tables inherit
// scoping through their visit.

public class OpVisitConfiguration : IEntityTypeConfiguration<OpVisit>
{
    public void Configure(EntityTypeBuilder<OpVisit> builder)
    {
        builder.ToTable("op_visits", "opd");
        builder.Property(v => v.VisitNumber).HasMaxLength(30);
        builder.Property(v => v.ConsultationFee).HasPrecision(18, 2);
        builder.Property(v => v.CancelReason).HasMaxLength(500);

        builder.HasIndex(v => new { v.BranchId, v.VisitNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(v => new { v.PatientId, v.DoctorId, v.VisitDate });
        builder.HasIndex(v => new { v.PatientId, v.MhcDoctorId, v.VisitDate });
        builder.HasIndex(v => new { v.BranchId, v.VisitDate, v.Status });
        builder.HasIndex(v => v.AppointmentId).IsUnique().HasFilter("is_deleted = false AND appointment_id IS NOT NULL");
        builder.HasIndex(v => v.CampId);

        builder.HasOne(v => v.Patient).WithMany()
            .HasForeignKey(v => v.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.Doctor).WithMany()
            .HasForeignKey(v => v.DoctorId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.MhcDoctor).WithMany()
            .HasForeignKey(v => v.MhcDoctorId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.MhcPackage).WithMany()
            .HasForeignKey(v => v.MhcPackageId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.Camp).WithMany()
            .HasForeignKey(v => v.CampId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.Appointment).WithMany()
            .HasForeignKey(v => v.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(v => v.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.Department).WithMany()
            .HasForeignKey(v => v.DepartmentId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.VisitClass).WithMany()
            .HasForeignKey(v => v.VisitClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.Session).WithMany()
            .HasForeignKey(v => v.SessionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VisitVitalsConfiguration : IEntityTypeConfiguration<VisitVitals>
{
    public void Configure(EntityTypeBuilder<VisitVitals> builder)
    {
        builder.ToTable("visit_vitals", "opd");
        builder.Property(v => v.HeightCm).HasPrecision(5, 1);
        builder.Property(v => v.WeightKg).HasPrecision(5, 1);
        builder.Property(v => v.Bmi).HasPrecision(4, 1);
        builder.Property(v => v.TemperatureF).HasPrecision(4, 1);
        builder.Property(v => v.RandomBloodSugar).HasPrecision(5, 1);
        builder.HasIndex(v => v.VisitId).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(v => v.Visit).WithMany()
            .HasForeignKey(v => v.VisitId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VisitComplaintConfiguration : IEntityTypeConfiguration<VisitComplaint>
{
    public void Configure(EntityTypeBuilder<VisitComplaint> builder)
    {
        builder.ToTable("visit_complaints", "opd");
        builder.Property(c => c.Complaint).HasMaxLength(300);
        builder.Property(c => c.Duration).HasMaxLength(50);
        builder.HasIndex(c => c.VisitId);
        builder.HasOne(c => c.Visit).WithMany()
            .HasForeignKey(c => c.VisitId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VisitExaminationConfiguration : IEntityTypeConfiguration<VisitExamination>
{
    public void Configure(EntityTypeBuilder<VisitExamination> builder)
    {
        builder.ToTable("visit_examinations", "opd");
        builder.Property(e => e.HistoryNotes).HasMaxLength(4000);
        builder.Property(e => e.ExaminationNotes).HasMaxLength(4000);
        builder.HasIndex(e => e.VisitId).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(e => e.Visit).WithMany()
            .HasForeignKey(e => e.VisitId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VisitDiagnosisConfiguration : IEntityTypeConfiguration<VisitDiagnosis>
{
    public void Configure(EntityTypeBuilder<VisitDiagnosis> builder)
    {
        builder.ToTable("visit_diagnoses", "opd");
        builder.Property(d => d.Notes).HasMaxLength(500);
        builder.HasIndex(d => d.VisitId);
        builder.HasOne(d => d.Visit).WithMany()
            .HasForeignKey(d => d.VisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.IcdCode).WithMany()
            .HasForeignKey(d => d.IcdCodeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("prescriptions", "opd");
        builder.Property(p => p.Notes).HasMaxLength(500);
        builder.HasIndex(p => p.VisitId).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(p => p.Visit).WithMany()
            .HasForeignKey(p => p.VisitId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("prescription_items", "opd");
        builder.Property(i => i.Dose).HasMaxLength(50);
        builder.Property(i => i.Frequency).HasMaxLength(50);
        builder.Property(i => i.Duration).HasMaxLength(50);
        builder.Property(i => i.Route).HasMaxLength(50);
        builder.Property(i => i.Instructions).HasMaxLength(300);
        builder.HasIndex(i => i.PrescriptionId);
        builder.HasOne(i => i.Prescription).WithMany(p => p.Items)
            .HasForeignKey(i => i.PrescriptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Drug).WithMany()
            .HasForeignKey(i => i.DrugId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PrescriptionTemplateConfiguration : IEntityTypeConfiguration<PrescriptionTemplate>
{
    public void Configure(EntityTypeBuilder<PrescriptionTemplate> builder)
    {
        builder.ToTable("prescription_templates", "opd");
        builder.Property(t => t.Name).HasMaxLength(100);
        builder.HasIndex(t => new { t.DoctorId, t.Name }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(t => t.Doctor).WithMany()
            .HasForeignKey(t => t.DoctorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PrescriptionTemplateItemConfiguration : IEntityTypeConfiguration<PrescriptionTemplateItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionTemplateItem> builder)
    {
        builder.ToTable("prescription_template_items", "opd");
        builder.Property(i => i.Dose).HasMaxLength(50);
        builder.Property(i => i.Frequency).HasMaxLength(50);
        builder.Property(i => i.Duration).HasMaxLength(50);
        builder.Property(i => i.Route).HasMaxLength(50);
        builder.Property(i => i.Instructions).HasMaxLength(300);
        builder.HasIndex(i => i.PrescriptionTemplateId);
        builder.HasOne(i => i.Template).WithMany(t => t.Items)
            .HasForeignKey(i => i.PrescriptionTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Drug).WithMany()
            .HasForeignKey(i => i.DrugId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClinicalOrderConfiguration : IEntityTypeConfiguration<ClinicalOrder>
{
    public void Configure(EntityTypeBuilder<ClinicalOrder> builder)
    {
        builder.ToTable("clinical_orders", "opd");
        builder.Property(o => o.Description).HasMaxLength(500);
        builder.Property(o => o.Notes).HasMaxLength(500);
        builder.HasIndex(o => new { o.VisitId, o.OrderType });
        builder.HasOne(o => o.Visit).WithMany()
            .HasForeignKey(o => o.VisitId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VisitReferralConfiguration : IEntityTypeConfiguration<VisitReferral>
{
    public void Configure(EntityTypeBuilder<VisitReferral> builder)
    {
        builder.ToTable("referrals", "opd");
        builder.Property(r => r.ExternalDetails).HasMaxLength(300);
        builder.Property(r => r.Reason).HasMaxLength(500);
        builder.Property(r => r.OpinionNotes).HasMaxLength(1000);
        builder.HasIndex(r => r.VisitId);
        builder.HasOne(r => r.Visit).WithMany()
            .HasForeignKey(r => r.VisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.ReferredToDoctor).WithMany()
            .HasForeignKey(r => r.ReferredToDoctorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FollowUpAdviceConfiguration : IEntityTypeConfiguration<FollowUpAdvice>
{
    public void Configure(EntityTypeBuilder<FollowUpAdvice> builder)
    {
        builder.ToTable("follow_ups", "opd");
        builder.Property(f => f.Notes).HasMaxLength(500);
        builder.HasIndex(f => f.VisitId).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(f => f.Visit).WithMany()
            .HasForeignKey(f => f.VisitId).OnDelete(DeleteBehavior.Restrict);
    }
}
