using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Mrd;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 17 MRD tables. Schema: mrd. RecordFile is branch-scoped (one file board per branch);
// every child table inherits scoping through it. Money numeric(18,2) per CLAUDE.md (not used
// in this module — no priced entities).

public class RecordFileConfiguration : IEntityTypeConfiguration<RecordFile>
{
    public void Configure(EntityTypeBuilder<RecordFile> builder)
    {
        builder.ToTable("record_files", "mrd");
        builder.Property(f => f.FileNumber).HasMaxLength(30);
        builder.Property(f => f.CurrentLocation).HasMaxLength(200);
        builder.HasIndex(f => new { f.BranchId, f.FileNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(f => f.AdmissionId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(f => f.Admission).WithMany().HasForeignKey(f => f.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.CurrentCustodianUser).WithMany().HasForeignKey(f => f.CurrentCustodianUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.CreatedByUser).WithMany().HasForeignKey(f => f.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(f => f.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FileMovementConfiguration : IEntityTypeConfiguration<FileMovement>
{
    public void Configure(EntityTypeBuilder<FileMovement> builder)
    {
        builder.ToTable("file_movements", "mrd");
        builder.Property(m => m.FromLocation).HasMaxLength(200);
        builder.Property(m => m.ToLocation).HasMaxLength(200);
        builder.Property(m => m.Reason).HasMaxLength(500);
        builder.HasIndex(m => new { m.RecordFileId, m.MovedAt });

        builder.HasOne(m => m.RecordFile).WithMany().HasForeignKey(m => m.RecordFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.FromCustodianUser).WithMany().HasForeignKey(m => m.FromCustodianUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.ToCustodianUser).WithMany().HasForeignKey(m => m.ToCustodianUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.MovedByUser).WithMany().HasForeignKey(m => m.MovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FileRequestConfiguration : IEntityTypeConfiguration<FileRequest>
{
    public void Configure(EntityTypeBuilder<FileRequest> builder)
    {
        builder.ToTable("file_requests", "mrd");
        builder.Property(r => r.RequesterName).HasMaxLength(200);
        builder.Property(r => r.RequesterOrganization).HasMaxLength(200);
        builder.Property(r => r.Purpose).HasMaxLength(500);
        builder.Property(r => r.RejectReason).HasMaxLength(500);
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.HasIndex(r => new { r.RecordFileId, r.Status });

        builder.HasOne(r => r.RecordFile).WithMany().HasForeignKey(r => r.RecordFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.RequestedByUser).WithMany().HasForeignKey(r => r.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.ApprovedByUser).WithMany().HasForeignKey(r => r.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.IssuedByUser).WithMany().HasForeignKey(r => r.IssuedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.ReturnedToUser).WithMany().HasForeignKey(r => r.ReturnedToUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ScannedDocumentConfiguration : IEntityTypeConfiguration<ScannedDocument>
{
    public void Configure(EntityTypeBuilder<ScannedDocument> builder)
    {
        builder.ToTable("scanned_documents", "mrd");
        builder.Property(d => d.FileName).HasMaxLength(260);
        builder.Property(d => d.FilePath).HasMaxLength(500);
        builder.Property(d => d.ContentType).HasMaxLength(150);
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.HasIndex(d => d.AdmissionId);

        builder.HasOne(d => d.Admission).WithMany().HasForeignKey(d => d.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.DocumentType).WithMany().HasForeignKey(d => d.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.UploadedByUser).WithMany().HasForeignKey(d => d.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CodingWorklistEntryConfiguration : IEntityTypeConfiguration<CodingWorklistEntry>
{
    public void Configure(EntityTypeBuilder<CodingWorklistEntry> builder)
    {
        builder.ToTable("coding_worklist", "mrd");
        builder.Property(c => c.Notes).HasMaxLength(1000);
        builder.HasIndex(c => c.AdmissionId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(c => c.Admission).WithMany().HasForeignKey(c => c.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.CodedByUser).WithMany().HasForeignKey(c => c.CodedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FileAuditConfiguration : IEntityTypeConfiguration<FileAudit>
{
    public void Configure(EntityTypeBuilder<FileAudit> builder)
    {
        builder.ToTable("file_audits", "mrd");
        builder.Property(a => a.MissingDocumentsNotes).HasMaxLength(1000);
        builder.HasIndex(a => a.AdmissionId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(a => a.Admission).WithMany().HasForeignKey(a => a.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.AuditedByUser).WithMany().HasForeignKey(a => a.AuditedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
    public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
    {
        builder.ToTable("retention_policies", "mrd");
        builder.Property(p => p.Code).HasMaxLength(30);
        builder.Property(p => p.Name).HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.HasIndex(p => p.Code).IsUnique().HasFilter("is_deleted = false");
    }
}

public class DestructionRegisterEntryConfiguration : IEntityTypeConfiguration<DestructionRegisterEntry>
{
    public void Configure(EntityTypeBuilder<DestructionRegisterEntry> builder)
    {
        builder.ToTable("destruction_register", "mrd");
        builder.Property(d => d.DestructionMethod).HasMaxLength(200);
        builder.Property(d => d.Notes).HasMaxLength(500);
        builder.HasIndex(d => d.RecordFileId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(d => d.RecordFile).WithMany().HasForeignKey(d => d.RecordFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.RetentionPolicy).WithMany().HasForeignKey(d => d.RetentionPolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.ApprovedByUser).WithMany().HasForeignKey(d => d.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.DestroyedByUser).WithMany().HasForeignKey(d => d.DestroyedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
