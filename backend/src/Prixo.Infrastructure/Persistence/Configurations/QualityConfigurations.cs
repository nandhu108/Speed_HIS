using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Quality;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 20 Quality/NABH tables. Schema: quality. Money numeric(18,2), quantities numeric(18,3)
// per CLAUDE.md — indicator Numerator/Denominator use numeric(18,3) since they're often counts,
// not money.

public class QualityIndicatorConfiguration : IEntityTypeConfiguration<QualityIndicator>
{
    public void Configure(EntityTypeBuilder<QualityIndicator> builder)
    {
        builder.ToTable("indicators", "quality");
        builder.Property(i => i.Code).HasMaxLength(30);
        builder.Property(i => i.Name).HasMaxLength(200);
        builder.Property(i => i.Category).HasMaxLength(100);
        builder.Property(i => i.NumeratorDefinition).HasMaxLength(500);
        builder.Property(i => i.DenominatorDefinition).HasMaxLength(500);
        builder.Property(i => i.Unit).HasMaxLength(30);
        builder.Property(i => i.TargetValue).HasPrecision(18, 3);
        builder.HasIndex(i => i.Code).IsUnique().HasFilter("is_deleted = false");
    }
}

public class QualityIndicatorEntryConfiguration : IEntityTypeConfiguration<QualityIndicatorEntry>
{
    public void Configure(EntityTypeBuilder<QualityIndicatorEntry> builder)
    {
        builder.ToTable("indicator_entries", "quality");
        builder.Property(e => e.Numerator).HasPrecision(18, 3);
        builder.Property(e => e.Denominator).HasPrecision(18, 3);
        builder.Property(e => e.Remarks).HasMaxLength(1000);
        builder.HasIndex(e => new { e.IndicatorId, e.BranchId, e.PeriodMonth }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(e => e.Indicator).WithMany().HasForeignKey(e => e.IndicatorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.EnteredByUser).WithMany().HasForeignKey(e => e.EnteredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QualityAuditTemplateConfiguration : IEntityTypeConfiguration<QualityAuditTemplate>
{
    public void Configure(EntityTypeBuilder<QualityAuditTemplate> builder)
    {
        builder.ToTable("audit_templates", "quality");
        builder.Property(t => t.Code).HasMaxLength(30);
        builder.Property(t => t.Name).HasMaxLength(200);
        builder.Property(t => t.Category).HasMaxLength(100);
        builder.HasIndex(t => t.Code).IsUnique().HasFilter("is_deleted = false");
        builder.HasMany(t => t.Items).WithOne(i => i.Template).HasForeignKey(i => i.TemplateId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QualityAuditTemplateItemConfiguration : IEntityTypeConfiguration<QualityAuditTemplateItem>
{
    public void Configure(EntityTypeBuilder<QualityAuditTemplateItem> builder)
    {
        builder.ToTable("audit_template_items", "quality");
        builder.Property(i => i.Question).HasMaxLength(500);
        builder.Property(i => i.MaxScore).HasPrecision(18, 3);
        builder.HasIndex(i => new { i.TemplateId, i.SortOrder });
    }
}

public class QualityAuditConfiguration : IEntityTypeConfiguration<QualityAudit>
{
    public void Configure(EntityTypeBuilder<QualityAudit> builder)
    {
        builder.ToTable("audits", "quality");
        builder.Property(a => a.Remarks).HasMaxLength(1000);
        builder.HasIndex(a => new { a.BranchId, a.Status, a.ScheduledDate });

        builder.HasOne(a => a.Template).WithMany().HasForeignKey(a => a.TemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.ConductedByUser).WithMany().HasForeignKey(a => a.ConductedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(a => a.Responses).WithOne(r => r.Audit).HasForeignKey(r => r.AuditId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QualityAuditResponseConfiguration : IEntityTypeConfiguration<QualityAuditResponse>
{
    public void Configure(EntityTypeBuilder<QualityAuditResponse> builder)
    {
        builder.ToTable("audit_responses", "quality");
        builder.Property(r => r.Score).HasPrecision(18, 3);
        builder.Property(r => r.Remarks).HasMaxLength(500);
        builder.HasIndex(r => new { r.AuditId, r.TemplateItemId }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(r => r.TemplateItem).WithMany().HasForeignKey(r => r.TemplateItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QualityNonConformanceConfiguration : IEntityTypeConfiguration<QualityNonConformance>
{
    public void Configure(EntityTypeBuilder<QualityNonConformance> builder)
    {
        builder.ToTable("non_conformances", "quality");
        builder.Property(n => n.Title).HasMaxLength(200);
        builder.Property(n => n.Description).HasMaxLength(2000);
        builder.HasIndex(n => new { n.BranchId, n.Status });

        builder.HasOne(n => n.Standard).WithMany().HasForeignKey(n => n.StandardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.Department).WithMany().HasForeignKey(n => n.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.RaisedByUser).WithMany().HasForeignKey(n => n.RaisedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.ClosedByUser).WithMany().HasForeignKey(n => n.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(n => n.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QualityRiskConfiguration : IEntityTypeConfiguration<QualityRisk>
{
    public void Configure(EntityTypeBuilder<QualityRisk> builder)
    {
        builder.ToTable("risks", "quality");
        builder.Property(r => r.Title).HasMaxLength(200);
        builder.Property(r => r.Category).HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(2000);
        builder.Property(r => r.MitigationPlan).HasMaxLength(2000);
        builder.HasIndex(r => new { r.BranchId, r.Status });

        builder.HasOne(r => r.OwnerUser).WithMany().HasForeignKey(r => r.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QualityCapaConfiguration : IEntityTypeConfiguration<QualityCapa>
{
    public void Configure(EntityTypeBuilder<QualityCapa> builder)
    {
        builder.ToTable("capas", "quality");
        builder.Property(c => c.ActionPlan).HasMaxLength(2000);
        builder.Property(c => c.VerificationNotes).HasMaxLength(1000);
        builder.HasIndex(c => new { c.SourceType, c.SourceId });
        builder.HasIndex(c => new { c.BranchId, c.Status, c.DueDate });

        builder.HasOne(c => c.OwnerUser).WithMany().HasForeignKey(c => c.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.VerifiedByUser).WithMany().HasForeignKey(c => c.VerifiedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(c => c.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QualityCommitteeConfiguration : IEntityTypeConfiguration<QualityCommittee>
{
    public void Configure(EntityTypeBuilder<QualityCommittee> builder)
    {
        builder.ToTable("committees", "quality");
        builder.Property(c => c.Code).HasMaxLength(30);
        builder.Property(c => c.Name).HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.HasIndex(c => c.Code).IsUnique().HasFilter("is_deleted = false");
    }
}

public class QualityCommitteeMeetingConfiguration : IEntityTypeConfiguration<QualityCommitteeMeeting>
{
    public void Configure(EntityTypeBuilder<QualityCommitteeMeeting> builder)
    {
        builder.ToTable("committee_meetings", "quality");
        builder.Property(m => m.Minutes).HasMaxLength(4000);
        builder.Property(m => m.AttendeesText).HasMaxLength(1000);
        builder.HasIndex(m => new { m.CommitteeId, m.MeetingDate });

        builder.HasOne(m => m.Committee).WithMany().HasForeignKey(m => m.CommitteeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(m => m.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(m => m.ActionItems).WithOne(a => a.Meeting).HasForeignKey(a => a.MeetingId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QualityCommitteeActionItemConfiguration : IEntityTypeConfiguration<QualityCommitteeActionItem>
{
    public void Configure(EntityTypeBuilder<QualityCommitteeActionItem> builder)
    {
        builder.ToTable("committee_action_items", "quality");
        builder.Property(a => a.Description).HasMaxLength(500);

        builder.HasOne(a => a.OwnerUser).WithMany().HasForeignKey(a => a.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QualityNabhStandardConfiguration : IEntityTypeConfiguration<QualityNabhStandard>
{
    public void Configure(EntityTypeBuilder<QualityNabhStandard> builder)
    {
        builder.ToTable("nabh_standards", "quality");
        builder.Property(s => s.Code).HasMaxLength(30);
        builder.Property(s => s.Name).HasMaxLength(200);
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.HasIndex(s => s.Code).IsUnique().HasFilter("is_deleted = false");
    }
}

public class QualityEvidenceConfiguration : IEntityTypeConfiguration<QualityEvidence>
{
    public void Configure(EntityTypeBuilder<QualityEvidence> builder)
    {
        builder.ToTable("evidence", "quality");
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.FilePath).HasMaxLength(500);
        builder.Property(e => e.FileName).HasMaxLength(260);
        builder.HasIndex(e => e.StandardId);

        builder.HasOne(e => e.Standard).WithMany().HasForeignKey(e => e.StandardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.UploadedByUser).WithMany().HasForeignKey(e => e.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
