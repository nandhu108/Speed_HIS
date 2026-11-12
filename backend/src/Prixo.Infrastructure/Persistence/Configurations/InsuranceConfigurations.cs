using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Insurance;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 24 Insurance/TPA module. Schema: insurance.

public class PatientInsurancePolicyConfiguration : IEntityTypeConfiguration<PatientInsurancePolicy>
{
    public void Configure(EntityTypeBuilder<PatientInsurancePolicy> builder)
    {
        builder.ToTable("patient_insurance_policies", "insurance");
        builder.Property(p => p.PolicyNumber).HasMaxLength(50);
        builder.HasIndex(p => p.PatientId);
        builder.HasOne(p => p.Patient).WithMany(p => p.InsurancePolicies)
            .HasForeignKey(p => p.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.InsuranceCompany).WithMany()
            .HasForeignKey(p => p.InsuranceCompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PreAuthRequestConfiguration : IEntityTypeConfiguration<PreAuthRequest>
{
    public void Configure(EntityTypeBuilder<PreAuthRequest> builder)
    {
        builder.ToTable("pre_auth_requests", "insurance");
        builder.Property(p => p.PreAuthNumber).HasMaxLength(30);
        builder.Property(p => p.Diagnosis).HasMaxLength(1000);
        builder.Property(p => p.RejectionReason).HasMaxLength(500);
        builder.Property(p => p.EstimatedAmount).HasColumnType("numeric(18,2)");
        builder.Property(p => p.ApprovedAmount).HasColumnType("numeric(18,2)");

        builder.HasIndex(p => new { p.BranchId, p.PreAuthNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(p => p.PatientId);
        builder.HasIndex(p => p.Status);

        builder.HasOne(p => p.Patient).WithMany()
            .HasForeignKey(p => p.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Policy).WithMany()
            .HasForeignKey(p => p.PolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.OpVisit).WithMany()
            .HasForeignKey(p => p.OpVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Admission).WithMany()
            .HasForeignKey(p => p.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.RequestedByUser).WithMany()
            .HasForeignKey(p => p.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.DecidedByUser).WithMany()
            .HasForeignKey(p => p.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PreAuthEnhancementConfiguration : IEntityTypeConfiguration<PreAuthEnhancement>
{
    public void Configure(EntityTypeBuilder<PreAuthEnhancement> builder)
    {
        builder.ToTable("pre_auth_enhancements", "insurance");
        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.Property(e => e.RejectionReason).HasMaxLength(500);
        builder.Property(e => e.RequestedAmount).HasColumnType("numeric(18,2)");
        builder.HasIndex(e => e.PreAuthRequestId);

        builder.HasOne(e => e.PreAuthRequest).WithMany(p => p.Enhancements)
            .HasForeignKey(e => e.PreAuthRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.RequestedByUser).WithMany()
            .HasForeignKey(e => e.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.DecidedByUser).WithMany()
            .HasForeignKey(e => e.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("claims", "insurance");
        builder.Property(c => c.ClaimNumber).HasMaxLength(30);
        builder.Property(c => c.DenialNotes).HasMaxLength(1000);
        builder.Property(c => c.SettlementReferenceNumber).HasMaxLength(100);
        builder.Property(c => c.ClaimAmount).HasColumnType("numeric(18,2)");
        builder.Property(c => c.ApprovedAmount).HasColumnType("numeric(18,2)");
        builder.Property(c => c.SettledAmount).HasColumnType("numeric(18,2)");
        builder.Property(c => c.DeductionAmount).HasColumnType("numeric(18,2)");

        builder.HasIndex(c => new { c.BranchId, c.ClaimNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(c => c.PatientId);
        builder.HasIndex(c => c.BillId);
        builder.HasIndex(c => c.Status);

        builder.HasOne(c => c.Patient).WithMany()
            .HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Policy).WithMany()
            .HasForeignKey(c => c.PolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.PreAuthRequest).WithMany()
            .HasForeignKey(c => c.PreAuthRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Bill).WithMany()
            .HasForeignKey(c => c.BillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.DenialReason).WithMany()
            .HasForeignKey(c => c.DenialReasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.SubmittedByUser).WithMany()
            .HasForeignKey(c => c.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.DecidedByUser).WithMany()
            .HasForeignKey(c => c.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClaimDocumentConfiguration : IEntityTypeConfiguration<ClaimDocument>
{
    public void Configure(EntityTypeBuilder<ClaimDocument> builder)
    {
        builder.ToTable("claim_documents", "insurance");
        builder.Property(d => d.FileName).HasMaxLength(300);
        builder.Property(d => d.FilePath).HasMaxLength(1000);
        builder.Property(d => d.ContentType).HasMaxLength(100);
        builder.HasIndex(d => d.ClaimId);

        builder.HasOne(d => d.Claim).WithMany(c => c.Documents)
            .HasForeignKey(d => d.ClaimId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.DocumentType).WithMany()
            .HasForeignKey(d => d.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.UploadedByUser).WithMany()
            .HasForeignKey(d => d.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
