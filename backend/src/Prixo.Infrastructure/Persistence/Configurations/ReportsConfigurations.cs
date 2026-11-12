using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Reports;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 23 Reports & Dashboards. Schema: reporting (both the catalog tables and the
// materialized views the keyless query types below map onto — plan §17: "Reporting
// reads from SQL views / materialized views (`reporting` schema)"). Global, not
// branch-scoped — the catalog is system-wide; the view rows carry branch_id as a plain
// column so callers filter by it themselves (management dashboards need cross-branch
// rows when REPORT.CROSSBRANCH.VIEW is held).

public class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("report_definitions", "reporting");
        builder.Property(r => r.Code).HasMaxLength(60);
        builder.Property(r => r.Name).HasMaxLength(200);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.Category).HasMaxLength(60);
        builder.Property(r => r.PermissionCode).HasMaxLength(100);
        builder.Property(r => r.ViewName).HasMaxLength(100);
        builder.Property(r => r.LinkPath).HasMaxLength(200);
        builder.Property(r => r.ColumnsJson).HasColumnType("jsonb");
        builder.HasIndex(r => r.Code).IsUnique().HasFilter("is_deleted = false");
    }
}

public class ReportSubscriptionConfiguration : IEntityTypeConfiguration<ReportSubscription>
{
    public void Configure(EntityTypeBuilder<ReportSubscription> builder)
    {
        builder.ToTable("report_subscriptions", "reporting");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ReportCode).HasMaxLength(60);
        builder.Property(s => s.CronExpression).HasMaxLength(60);
        builder.Property(s => s.RecipientEmail).HasMaxLength(200);
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.ReportCode);
    }
}

// ── Keyless query types mapped onto the materialized views (never migrated as tables — see the raw-SQL migration that creates them) ──

public class RevenueDailyRowConfiguration : IEntityTypeConfiguration<RevenueDailyRow>
{
    public void Configure(EntityTypeBuilder<RevenueDailyRow> builder)
    {
        builder.HasNoKey();
        builder.ToView("mv_revenue_daily", "reporting");
    }
}

public class DailyCollectionRowConfiguration : IEntityTypeConfiguration<DailyCollectionRow>
{
    public void Configure(EntityTypeBuilder<DailyCollectionRow> builder)
    {
        builder.HasNoKey();
        builder.ToView("mv_daily_collection", "reporting");
    }
}

public class OpCensusRowConfiguration : IEntityTypeConfiguration<OpCensusRow>
{
    public void Configure(EntityTypeBuilder<OpCensusRow> builder)
    {
        builder.HasNoKey();
        builder.ToView("mv_op_census", "reporting");
    }
}
