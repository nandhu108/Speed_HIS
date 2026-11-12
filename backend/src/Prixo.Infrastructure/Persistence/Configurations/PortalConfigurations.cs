using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Patients;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 21 Patient Portal additions. Schema: patient (no dedicated "portal" schema in the
// plan's schema list — portal identity is anchored on Patient). Global, not branch-scoped.

public class PortalAccountConfiguration : IEntityTypeConfiguration<PortalAccount>
{
    public void Configure(EntityTypeBuilder<PortalAccount> builder)
    {
        builder.ToTable("portal_accounts", "patient");
        builder.Property(a => a.PasswordHash).HasMaxLength(500);
        builder.HasIndex(a => a.PatientId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PortalRefreshTokenConfiguration : IEntityTypeConfiguration<PortalRefreshToken>
{
    public void Configure(EntityTypeBuilder<PortalRefreshToken> builder)
    {
        builder.ToTable("portal_refresh_tokens", "patient");
        builder.Property(t => t.TokenHash).HasMaxLength(200);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(200);
        builder.HasIndex(t => t.TokenHash).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(t => t.PatientId);

        builder.HasOne(t => t.Patient).WithMany().HasForeignKey(t => t.PatientId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("feedbacks", "patient");
        builder.Property(f => f.Comments).HasMaxLength(1000);
        builder.HasIndex(f => f.PatientId);

        builder.HasOne(f => f.Patient).WithMany().HasForeignKey(f => f.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.OpVisit).WithMany().HasForeignKey(f => f.OpVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.Admission).WithMany().HasForeignKey(f => f.AdmissionId).OnDelete(DeleteBehavior.Restrict);
    }
}
