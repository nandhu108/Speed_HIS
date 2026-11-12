using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Patients;

namespace Prixo.Infrastructure.Persistence.Configurations;

public class AbhaProfileConfiguration : IEntityTypeConfiguration<AbhaProfile>
{
    public void Configure(EntityTypeBuilder<AbhaProfile> builder)
    {
        builder.ToTable("abha_profiles", "patient");
        builder.Property(p => p.AbhaNumber).HasMaxLength(20);
        builder.Property(p => p.AbhaAddress).HasMaxLength(100);
        builder.Property(p => p.LinkedName).HasMaxLength(200);
        builder.Property(p => p.LinkedGender).HasMaxLength(20);

        builder.HasOne(p => p.Patient).WithMany().HasForeignKey(p => p.PatientId).OnDelete(DeleteBehavior.Restrict);

        // One active ABHA per patient; the ABHA number is globally unique. Both partial on soft-delete.
        builder.HasIndex(p => p.PatientId).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(p => p.AbhaNumber).IsUnique().HasFilter("is_deleted = false");
    }
}

public class AbdmVerificationSessionConfiguration : IEntityTypeConfiguration<AbdmVerificationSession>
{
    public void Configure(EntityTypeBuilder<AbdmVerificationSession> builder)
    {
        builder.ToTable("abdm_verification_sessions", "patient");
        builder.Property(s => s.AbdmTxnId).HasMaxLength(100);
        builder.Property(s => s.MaskedTarget).HasMaxLength(50);

        builder.HasOne(s => s.Patient).WithMany().HasForeignKey(s => s.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => s.PatientId);
    }
}

public class AbdmCareContextConfiguration : IEntityTypeConfiguration<AbdmCareContext>
{
    public void Configure(EntityTypeBuilder<AbdmCareContext> builder)
    {
        builder.ToTable("abdm_care_contexts", "patient");
        builder.Property(c => c.PatientReference).HasMaxLength(50);
        builder.Property(c => c.AbhaNumber).HasMaxLength(20);
        builder.Property(c => c.ReferenceNumber).HasMaxLength(100);
        builder.Property(c => c.Display).HasMaxLength(200);

        builder.HasOne(c => c.Patient).WithMany().HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => new { c.PatientId, c.ReferenceNumber }).IsUnique().HasFilter("is_deleted = false");
    }
}

public class AbdmDataTransferLogConfiguration : IEntityTypeConfiguration<AbdmDataTransferLog>
{
    public void Configure(EntityTypeBuilder<AbdmDataTransferLog> builder)
    {
        builder.ToTable("abdm_data_transfer_logs", "patient");
        builder.Property(l => l.TransactionId).HasMaxLength(100);
        builder.Property(l => l.ConsentArtifactId).HasMaxLength(100);
        builder.Property(l => l.CareContextReference).HasMaxLength(100);
        builder.Property(l => l.RecipientHiuId).HasMaxLength(100);
        builder.Property(l => l.FhirBundleJson).HasColumnType("jsonb");
        builder.Property(l => l.ErrorMessage).HasMaxLength(500);

        builder.HasIndex(l => l.TransactionId);
        builder.HasIndex(l => l.ConsentArtifactId);
    }
}

public class AbdmConsentRequestConfiguration : IEntityTypeConfiguration<AbdmConsentRequest>
{
    public void Configure(EntityTypeBuilder<AbdmConsentRequest> builder)
    {
        builder.ToTable("abdm_consent_requests", "patient");
        builder.Property(r => r.ConsentRequestId).HasMaxLength(100);
        builder.Property(r => r.ConsentArtifactId).HasMaxLength(100);
        builder.Property(r => r.AbhaNumber).HasMaxLength(20);
        builder.Property(r => r.AbhaAddress).HasMaxLength(100);
        builder.Property(r => r.Purpose).HasMaxLength(50);
        builder.Property(r => r.HiTypes).HasMaxLength(200);
        builder.Property(r => r.RequesterDoctorName).HasMaxLength(100);

        builder.HasOne(r => r.Patient).WithMany().HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.ConsentRequestId).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(r => new { r.PatientId, r.Status });
    }
}

public class AbdmExternalHealthRecordConfiguration : IEntityTypeConfiguration<AbdmExternalHealthRecord>
{
    public void Configure(EntityTypeBuilder<AbdmExternalHealthRecord> builder)
    {
        builder.ToTable("abdm_external_health_records", "patient");
        builder.Property(h => h.SourceFacilityName).HasMaxLength(200);
        builder.Property(h => h.SourceFacilityHipId).HasMaxLength(100);
        builder.Property(h => h.DoctorName).HasMaxLength(100);
        builder.Property(h => h.Title).HasMaxLength(200);
        builder.Property(h => h.FhirBundleJson).HasColumnType("jsonb");

        builder.HasOne(h => h.Patient).WithMany().HasForeignKey(h => h.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.ConsentRequest).WithMany().HasForeignKey(h => h.ConsentRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(h => h.PatientId);
    }
}
