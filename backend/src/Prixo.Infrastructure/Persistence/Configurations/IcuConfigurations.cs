using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Icu;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 15 ICU tables. Schema: icu. Every table is branch-scoped directly or inherits
// scoping through AdmissionId (IPD). Money numeric(18,2), quantities numeric(18,3) per
// CLAUDE.md.

public class VentilatorSessionConfiguration : IEntityTypeConfiguration<VentilatorSession>
{
    public void Configure(EntityTypeBuilder<VentilatorSession> builder)
    {
        builder.ToTable("ventilator_sessions", "icu");
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.HasIndex(s => new { s.AdmissionId, s.StartedAt });

        builder.HasOne(s => s.Admission).WithMany().HasForeignKey(s => s.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.StartedByUser).WithMany().HasForeignKey(s => s.StartedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VentilatorObservationConfiguration : IEntityTypeConfiguration<VentilatorObservation>
{
    public void Configure(EntityTypeBuilder<VentilatorObservation> builder)
    {
        builder.ToTable("ventilator_observations", "icu");
        builder.Property(o => o.PeakPressure).HasPrecision(6, 2);
        builder.Property(o => o.PlateauPressure).HasPrecision(6, 2);
        builder.Property(o => o.Notes).HasMaxLength(500);
        builder.HasIndex(o => new { o.SessionId, o.RecordedAt });

        builder.HasOne(o => o.Session).WithMany(s => s.Observations).HasForeignKey(o => o.SessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.RecordedByUser).WithMany().HasForeignKey(o => o.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class InfusionChartEntryConfiguration : IEntityTypeConfiguration<InfusionChartEntry>
{
    public void Configure(EntityTypeBuilder<InfusionChartEntry> builder)
    {
        builder.ToTable("infusion_chart_entries", "icu");
        builder.Property(e => e.DrugNameOverride).HasMaxLength(150);
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.HasIndex(e => new { e.AdmissionId, e.StartedAt });

        builder.HasOne(e => e.Admission).WithMany().HasForeignKey(e => e.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Drug).WithMany().HasForeignKey(e => e.DrugId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.StartedByUser).WithMany().HasForeignKey(e => e.StartedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class InfusionRateEntryConfiguration : IEntityTypeConfiguration<InfusionRateEntry>
{
    public void Configure(EntityTypeBuilder<InfusionRateEntry> builder)
    {
        builder.ToTable("infusion_rate_entries", "icu");
        builder.Property(r => r.Rate).HasPrecision(10, 3);
        builder.Property(r => r.RateUnit).HasMaxLength(30);
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.HasIndex(r => new { r.InfusionChartEntryId, r.RecordedAt });

        builder.HasOne(r => r.InfusionChartEntry).WithMany(e => e.RateEntries).HasForeignKey(r => r.InfusionChartEntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.RecordedByUser).WithMany().HasForeignKey(r => r.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class IcuSeverityAssessmentConfiguration : IEntityTypeConfiguration<IcuSeverityAssessment>
{
    public void Configure(EntityTypeBuilder<IcuSeverityAssessment> builder)
    {
        builder.ToTable("icu_severity_assessments", "icu");
        builder.Property(a => a.Notes).HasMaxLength(500);
        builder.HasIndex(a => new { a.AdmissionId, a.AssessedAt });

        builder.HasOne(a => a.Admission).WithMany().HasForeignKey(a => a.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.AssessedByUser).WithMany().HasForeignKey(a => a.AssessedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CriticalAlertRuleConfiguration : IEntityTypeConfiguration<CriticalAlertRule>
{
    public void Configure(EntityTypeBuilder<CriticalAlertRule> builder)
    {
        builder.ToTable("critical_alert_rules", "icu");
        builder.Property(r => r.ThresholdValue).HasPrecision(10, 3);
        builder.Property(r => r.Description).HasMaxLength(300);
        builder.HasIndex(r => new { r.BranchId, r.Parameter, r.Operator }).HasFilter("is_deleted = false");

        builder.HasOne<Branch>().WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class IcuVitalsAlertConfiguration : IEntityTypeConfiguration<IcuVitalsAlert>
{
    public void Configure(EntityTypeBuilder<IcuVitalsAlert> builder)
    {
        builder.ToTable("icu_vitals_alerts", "icu");
        builder.Property(a => a.AcknowledgementNotes).HasMaxLength(500);
        builder.HasIndex(a => new { a.AdmissionId, a.IsAcknowledged });

        builder.HasOne(a => a.Admission).WithMany().HasForeignKey(a => a.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.IpVitals).WithMany().HasForeignKey(a => a.IpVitalsId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.CriticalAlertRule).WithMany().HasForeignKey(a => a.CriticalAlertRuleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.AcknowledgedByUser).WithMany().HasForeignKey(a => a.AcknowledgedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DeviceReadingConfiguration : IEntityTypeConfiguration<DeviceReading>
{
    public void Configure(EntityTypeBuilder<DeviceReading> builder)
    {
        builder.ToTable("device_readings", "icu");
        builder.Property(d => d.DeviceType).HasMaxLength(50);
        builder.Property(d => d.ParameterName).HasMaxLength(50);
        builder.Property(d => d.Value).HasPrecision(12, 4);
        builder.Property(d => d.Unit).HasMaxLength(20);
        builder.HasIndex(d => new { d.AdmissionId, d.RecordedAt });

        builder.HasOne(d => d.Admission).WithMany().HasForeignKey(d => d.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(d => d.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
