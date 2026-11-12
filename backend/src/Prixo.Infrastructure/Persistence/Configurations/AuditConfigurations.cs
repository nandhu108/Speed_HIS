using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Audit;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Append-only log tables. Schema: audit. No soft delete, no updates.

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", "audit");
        builder.Property(l => l.EntityName).HasMaxLength(100);
        builder.Property(l => l.EntityId).HasMaxLength(100);
        builder.Property(l => l.Action).HasMaxLength(20);
        builder.Property(l => l.OldValues).HasColumnType("jsonb");
        builder.Property(l => l.NewValues).HasColumnType("jsonb");
        builder.Property(l => l.TraceId).HasMaxLength(100);
        builder.Property(l => l.IpAddress).HasMaxLength(45);
        builder.HasIndex(l => new { l.EntityName, l.EntityId });
        builder.HasIndex(l => l.UserId);
        builder.HasIndex(l => l.CreatedAt);
    }
}

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activity_logs", "audit");
        builder.Property(l => l.Username).HasMaxLength(100);
        builder.Property(l => l.EventType).HasMaxLength(50);
        builder.Property(l => l.Description).HasMaxLength(1000);
        builder.Property(l => l.IpAddress).HasMaxLength(45);
        builder.Property(l => l.UserAgent).HasMaxLength(500);
        builder.HasIndex(l => l.EventType);
        builder.HasIndex(l => l.UserId);
        builder.HasIndex(l => l.CreatedAt);
    }
}
