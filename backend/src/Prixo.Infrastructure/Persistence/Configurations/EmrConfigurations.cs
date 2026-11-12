using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Emr;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 11 EMR tables. Schema: emr. Every table is patient-scoped (BaseEntity, no BranchId) —
// clinical safety data (histories, allergies) must read the same regardless of which branch a
// doctor is viewing the patient from, per CLAUDE.md's "Patients... are global" rule.

public class MedicalHistoryConfiguration : IEntityTypeConfiguration<MedicalHistory>
{
    public void Configure(EntityTypeBuilder<MedicalHistory> builder)
    {
        builder.ToTable("medical_histories", "emr");
        builder.Property(h => h.Condition).HasMaxLength(200);
        builder.Property(h => h.Notes).HasMaxLength(1000);
        builder.HasIndex(h => h.PatientId);

        builder.HasOne(h => h.Patient).WithMany().HasForeignKey(h => h.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.RecordedByUser).WithMany().HasForeignKey(h => h.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SurgicalHistoryConfiguration : IEntityTypeConfiguration<SurgicalHistory>
{
    public void Configure(EntityTypeBuilder<SurgicalHistory> builder)
    {
        builder.ToTable("surgical_histories", "emr");
        builder.Property(h => h.ProcedureName).HasMaxLength(200);
        builder.Property(h => h.Surgeon).HasMaxLength(200);
        builder.Property(h => h.Notes).HasMaxLength(1000);
        builder.HasIndex(h => h.PatientId);

        builder.HasOne(h => h.Patient).WithMany().HasForeignKey(h => h.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.RecordedByUser).WithMany().HasForeignKey(h => h.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FamilyHistoryConfiguration : IEntityTypeConfiguration<FamilyHistory>
{
    public void Configure(EntityTypeBuilder<FamilyHistory> builder)
    {
        builder.ToTable("family_histories", "emr");
        builder.Property(h => h.Relation).HasMaxLength(50);
        builder.Property(h => h.Condition).HasMaxLength(200);
        builder.Property(h => h.Notes).HasMaxLength(1000);
        builder.HasIndex(h => h.PatientId);

        builder.HasOne(h => h.Patient).WithMany().HasForeignKey(h => h.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.RecordedByUser).WithMany().HasForeignKey(h => h.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AllergyConfiguration : IEntityTypeConfiguration<Allergy>
{
    public void Configure(EntityTypeBuilder<Allergy> builder)
    {
        builder.ToTable("allergies", "emr");
        builder.Property(a => a.AllergenName).HasMaxLength(200);
        builder.Property(a => a.Reaction).HasMaxLength(500);
        builder.HasIndex(a => new { a.PatientId, a.IsActive });

        builder.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.RecordedByUser).WithMany().HasForeignKey(a => a.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImmunizationConfiguration : IEntityTypeConfiguration<Immunization>
{
    public void Configure(EntityTypeBuilder<Immunization> builder)
    {
        builder.ToTable("immunizations", "emr");
        builder.Property(i => i.VaccineName).HasMaxLength(200);
        builder.Property(i => i.AdministeredBy).HasMaxLength(200);
        builder.Property(i => i.Notes).HasMaxLength(1000);
        builder.HasIndex(i => i.PatientId);

        builder.HasOne(i => i.Patient).WithMany().HasForeignKey(i => i.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.RecordedByUser).WithMany().HasForeignKey(i => i.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProblemListEntryConfiguration : IEntityTypeConfiguration<ProblemListEntry>
{
    public void Configure(EntityTypeBuilder<ProblemListEntry> builder)
    {
        builder.ToTable("problem_list", "emr");
        builder.Property(p => p.DiagnosisText).HasMaxLength(200);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.HasIndex(p => new { p.PatientId, p.Status });

        builder.HasOne(p => p.Patient).WithMany().HasForeignKey(p => p.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.IcdCode).WithMany().HasForeignKey(p => p.IcdCodeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.RecordedByUser).WithMany().HasForeignKey(p => p.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClinicalNoteConfiguration : IEntityTypeConfiguration<ClinicalNote>
{
    public void Configure(EntityTypeBuilder<ClinicalNote> builder)
    {
        builder.ToTable("clinical_notes", "emr");
        builder.Property(n => n.NoteType).HasMaxLength(100);
        builder.Property(n => n.Title).HasMaxLength(200);
        builder.Property(n => n.Content).HasMaxLength(8000);
        builder.HasIndex(n => new { n.PatientId, n.NoteAt });

        builder.HasOne(n => n.Patient).WithMany().HasForeignKey(n => n.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.RecordedByUser).WithMany().HasForeignKey(n => n.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.OpVisit).WithMany().HasForeignKey(n => n.OpVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.Admission).WithMany().HasForeignKey(n => n.AdmissionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("attachments", "emr");
        builder.Property(a => a.AttachmentType).HasMaxLength(100);
        builder.Property(a => a.FileName).HasMaxLength(260);
        builder.Property(a => a.FilePath).HasMaxLength(500);
        builder.Property(a => a.ContentType).HasMaxLength(100);
        builder.Property(a => a.Description).HasMaxLength(500);
        builder.HasIndex(a => a.PatientId);

        builder.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.UploadedByUser).WithMany().HasForeignKey(a => a.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
