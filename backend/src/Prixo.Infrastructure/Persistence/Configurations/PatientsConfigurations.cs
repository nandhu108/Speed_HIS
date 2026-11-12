using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 3 patient tables. Schema: patient. Patient itself is global (no BranchId) —
// UHID is the org-wide identity; RegisteredBranchId is informational only.

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients", "patient");
        builder.Property(p => p.Uhid).HasMaxLength(30);
        builder.Property(p => p.FirstName).HasMaxLength(100);
        builder.Property(p => p.MiddleName).HasMaxLength(100);
        builder.Property(p => p.LastName).HasMaxLength(100);
        builder.Property(p => p.BloodGroup).HasMaxLength(5);
        builder.Property(p => p.Mobile).HasMaxLength(20);
        builder.Property(p => p.AlternateMobile).HasMaxLength(20);
        builder.Property(p => p.Email).HasMaxLength(200);
        builder.Property(p => p.WhatsAppNumber).HasMaxLength(20);
        builder.Property(p => p.Address).HasMaxLength(500);
        builder.Property(p => p.PermanentAddress).HasMaxLength(500);
        builder.Property(p => p.Pincode).HasMaxLength(10);
        builder.Property(p => p.Occupation).HasMaxLength(100);
        builder.Property(p => p.MotherName).HasMaxLength(200);
        builder.Property(p => p.NationalId).HasMaxLength(50);
        builder.Property(p => p.Remarks).HasMaxLength(1000);
        builder.Property(p => p.PhotoPath).HasMaxLength(500);
        builder.Property(p => p.BlockedReason).HasMaxLength(500);
        builder.Property(p => p.MonthlyIncome).HasColumnType("numeric(18,2)");

        builder.HasIndex(p => p.Uhid).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(p => p.Mobile);
        builder.HasIndex(p => p.DateOfBirth);

        builder.HasOne(p => p.PatientCategory).WithMany()
            .HasForeignKey(p => p.PatientCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Referrer).WithMany()
            .HasForeignKey(p => p.ReferrerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.RegisteredBranch).WithMany()
            .HasForeignKey(p => p.RegisteredBranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.BlockedByUser).WithMany()
            .HasForeignKey(p => p.BlockedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Patient>().WithMany()
            .HasForeignKey(p => p.MergedIntoPatientId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Salutation).WithMany()
            .HasForeignKey(p => p.SalutationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.MaritalStatus).WithMany()
            .HasForeignKey(p => p.MaritalStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.State).WithMany()
            .HasForeignKey(p => p.StateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.City).WithMany()
            .HasForeignKey(p => p.CityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Area).WithMany()
            .HasForeignKey(p => p.AreaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Country).WithMany()
            .HasForeignKey(p => p.CountryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Nationality).WithMany()
            .HasForeignKey(p => p.NationalityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Language).WithMany()
            .HasForeignKey(p => p.LanguageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.PatientCaseType).WithMany()
            .HasForeignKey(p => p.PatientCaseTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.PatientHcTag).WithMany()
            .HasForeignKey(p => p.PatientHcTagId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.SpeciallyAbledCategory).WithMany()
            .HasForeignKey(p => p.SpeciallyAbledCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PatientContactConfiguration : IEntityTypeConfiguration<PatientContact>
{
    public void Configure(EntityTypeBuilder<PatientContact> builder)
    {
        builder.ToTable("patient_contacts", "patient");
        builder.Property(c => c.Name).HasMaxLength(200);
        builder.Property(c => c.Relationship).HasMaxLength(50);
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.HasIndex(c => c.PatientId);
        builder.HasOne(c => c.Patient).WithMany(p => p.Contacts)
            .HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PatientIdentifierConfiguration : IEntityTypeConfiguration<PatientIdentifier>
{
    public void Configure(EntityTypeBuilder<PatientIdentifier> builder)
    {
        builder.ToTable("patient_identifiers", "patient");
        builder.Property(i => i.Value).HasMaxLength(50);
        builder.HasIndex(i => i.PatientId);
        builder.HasOne(i => i.Patient).WithMany(p => p.Identifiers)
            .HasForeignKey(i => i.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.IdentityDocumentType).WithMany()
            .HasForeignKey(i => i.IdentityDocumentTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PatientDocumentConfiguration : IEntityTypeConfiguration<PatientDocument>
{
    public void Configure(EntityTypeBuilder<PatientDocument> builder)
    {
        builder.ToTable("patient_documents", "patient");
        builder.Property(d => d.FileName).HasMaxLength(300);
        builder.Property(d => d.FilePath).HasMaxLength(1000);
        builder.Property(d => d.ContentType).HasMaxLength(100);
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.HasIndex(d => d.PatientId);
        builder.HasOne(d => d.Patient).WithMany(p => p.Documents)
            .HasForeignKey(d => d.PatientId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PatientConsentConfiguration : IEntityTypeConfiguration<PatientConsent>
{
    public void Configure(EntityTypeBuilder<PatientConsent> builder)
    {
        builder.ToTable("patient_consents", "patient");
        builder.Property(c => c.SignedByName).HasMaxLength(200);
        builder.Property(c => c.RelationshipToPatient).HasMaxLength(50);
        builder.Property(c => c.DocumentPath).HasMaxLength(1000);
        builder.HasIndex(c => c.PatientId);
        builder.HasOne(c => c.Patient).WithMany(p => p.Consents)
            .HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.ConsentTemplate).WithMany()
            .HasForeignKey(c => c.ConsentTemplateId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PatientMergeConfiguration : IEntityTypeConfiguration<PatientMerge>
{
    public void Configure(EntityTypeBuilder<PatientMerge> builder)
    {
        builder.ToTable("patient_merges", "patient");
        builder.Property(m => m.Reason).HasMaxLength(500);
        builder.HasOne(m => m.SurvivorPatient).WithMany()
            .HasForeignKey(m => m.SurvivorPatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.MergedPatient).WithMany()
            .HasForeignKey(m => m.MergedPatientId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ConsentTemplateConfiguration : IEntityTypeConfiguration<ConsentTemplate>
{
    public void Configure(EntityTypeBuilder<ConsentTemplate> builder)
    {
        builder.ToTable("consent_templates", "master");
        builder.Property(c => c.Code).HasMaxLength(30);
        builder.Property(c => c.Title).HasMaxLength(200);
        builder.Property(c => c.BodyText).HasColumnType("text");
        builder.HasIndex(c => c.Code).IsUnique().HasFilter("is_deleted = false");
    }
}

public class SequenceCounterConfiguration : IEntityTypeConfiguration<SequenceCounter>
{
    public void Configure(EntityTypeBuilder<SequenceCounter> builder)
    {
        builder.ToTable("sequence_counters", "admin");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SequenceKey).HasMaxLength(50);
        builder.HasIndex(s => new { s.SequenceKey, s.ScopeId, s.Year }).IsUnique();
    }
}
