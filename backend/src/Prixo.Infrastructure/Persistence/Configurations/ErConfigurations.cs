using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Er;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 16 Emergency/Casualty tables. Schema: er. EmergencyVisit is branch-scoped; every
// child table inherits scoping through it. Money numeric(18,2), quantities numeric(18,3)
// per CLAUDE.md.

public class EmergencyVisitConfiguration : IEntityTypeConfiguration<EmergencyVisit>
{
    public void Configure(EntityTypeBuilder<EmergencyVisit> builder)
    {
        builder.ToTable("emergency_visits", "er");
        builder.Property(v => v.VisitNumber).HasMaxLength(30);
        builder.Property(v => v.CancelReason).HasMaxLength(500);
        builder.HasIndex(v => new { v.BranchId, v.VisitNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(v => new { v.BranchId, v.ArrivedAt, v.Status });

        builder.HasOne(v => v.Patient).WithMany().HasForeignKey(v => v.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.AssignedDoctor).WithMany().HasForeignKey(v => v.AssignedDoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.RegisteredByUser).WithMany().HasForeignKey(v => v.RegisteredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(v => v.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TriageConfiguration : IEntityTypeConfiguration<Triage>
{
    public void Configure(EntityTypeBuilder<Triage> builder)
    {
        builder.ToTable("triages", "er");
        builder.Property(t => t.ChiefComplaint).HasMaxLength(500);
        builder.Property(t => t.TemperatureF).HasPrecision(5, 2);
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.HasIndex(t => t.EmergencyVisitId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(t => t.EmergencyVisit).WithMany().HasForeignKey(t => t.EmergencyVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.TriagedByUser).WithMany().HasForeignKey(t => t.TriagedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmergencyAssessmentConfiguration : IEntityTypeConfiguration<EmergencyAssessment>
{
    public void Configure(EntityTypeBuilder<EmergencyAssessment> builder)
    {
        builder.ToTable("emergency_assessments", "er");
        builder.Property(a => a.History).HasMaxLength(2000);
        builder.Property(a => a.ExaminationFindings).HasMaxLength(2000);
        builder.Property(a => a.ProvisionalDiagnosis).HasMaxLength(500);
        builder.Property(a => a.TreatmentGiven).HasMaxLength(2000);
        builder.Property(a => a.DispositionNotes).HasMaxLength(500);
        builder.Property(a => a.TransferredToFacility).HasMaxLength(200);
        builder.Property(a => a.TransferSummary).HasMaxLength(2000);
        builder.HasIndex(a => a.EmergencyVisitId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(a => a.EmergencyVisit).WithMany().HasForeignKey(a => a.EmergencyVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.AssessedByDoctor).WithMany().HasForeignKey(a => a.AssessedByDoctorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MlcRecordConfiguration : IEntityTypeConfiguration<MlcRecord>
{
    public void Configure(EntityTypeBuilder<MlcRecord> builder)
    {
        builder.ToTable("mlc_records", "er");
        builder.Property(m => m.MlcNumber).HasMaxLength(30);
        builder.Property(m => m.PoliceStationName).HasMaxLength(150);
        builder.Property(m => m.FirNumber).HasMaxLength(50);
        builder.Property(m => m.BroughtByName).HasMaxLength(150);
        builder.Property(m => m.BroughtByDesignation).HasMaxLength(100);
        builder.Property(m => m.InjuryDescription).HasMaxLength(1000);
        builder.Property(m => m.Notes).HasMaxLength(500);
        builder.HasIndex(m => m.MlcNumber).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(m => m.EmergencyVisitId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(m => m.EmergencyVisit).WithMany().HasForeignKey(m => m.EmergencyVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.RecordedByUser).WithMany().HasForeignKey(m => m.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AccidentCaseRecordConfiguration : IEntityTypeConfiguration<AccidentCaseRecord>
{
    public void Configure(EntityTypeBuilder<AccidentCaseRecord> builder)
    {
        builder.ToTable("accident_case_records", "er");
        builder.Property(a => a.AccidentType).HasMaxLength(100);
        builder.Property(a => a.AccidentLocation).HasMaxLength(300);
        builder.Property(a => a.Notes).HasMaxLength(500);
        builder.HasIndex(a => a.EmergencyVisitId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(a => a.EmergencyVisit).WithMany().HasForeignKey(a => a.EmergencyVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.RecordedByUser).WithMany().HasForeignKey(a => a.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ObservationAllocationConfiguration : IEntityTypeConfiguration<ObservationAllocation>
{
    public void Configure(EntityTypeBuilder<ObservationAllocation> builder)
    {
        builder.ToTable("observation_allocations", "er");
        builder.HasIndex(o => o.EmergencyVisitId);
        builder.HasIndex(o => o.BedId).IsUnique().HasFilter("is_deleted = false AND released_at IS NULL");

        builder.HasOne(o => o.EmergencyVisit).WithMany().HasForeignKey(o => o.EmergencyVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Bed).WithMany().HasForeignKey(o => o.BedId).OnDelete(DeleteBehavior.Restrict);
    }
}
