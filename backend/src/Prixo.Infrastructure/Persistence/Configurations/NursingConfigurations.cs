using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Nursing;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 10 Nursing tables. Schema: nursing. Every table hangs off ipd.admissions (BaseEntity,
// no direct BranchId — same posture as IPD's ProgressNote/IpVitals) except ShiftHandover and
// Incident, which are branch-scoped directly like Admission/Bill.

public class NursingAssessmentConfiguration : IEntityTypeConfiguration<NursingAssessment>
{
    public void Configure(EntityTypeBuilder<NursingAssessment> builder)
    {
        builder.ToTable("nursing_assessments", "nursing");
        builder.Property(a => a.TemplateCode).HasMaxLength(50);
        builder.Property(a => a.DataJson).HasColumnType("jsonb");
        builder.HasIndex(a => new { a.AdmissionId, a.AssessedAt });

        builder.HasOne(a => a.Admission).WithMany().HasForeignKey(a => a.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.AssessedByUser).WithMany().HasForeignKey(a => a.AssessedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class NursingNoteConfiguration : IEntityTypeConfiguration<NursingNote>
{
    public void Configure(EntityTypeBuilder<NursingNote> builder)
    {
        builder.ToTable("nursing_notes", "nursing");
        builder.Property(n => n.Notes).HasMaxLength(4000);
        builder.HasIndex(n => new { n.AdmissionId, n.NoteAt });

        builder.HasOne(n => n.Admission).WithMany().HasForeignKey(n => n.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.RecordedByUser).WithMany().HasForeignKey(n => n.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RiskAssessmentConfiguration : IEntityTypeConfiguration<RiskAssessment>
{
    public void Configure(EntityTypeBuilder<RiskAssessment> builder)
    {
        builder.ToTable("risk_assessments", "nursing");
        builder.Property(r => r.ScoreDataJson).HasColumnType("jsonb");
        builder.Property(r => r.RiskLevel).HasMaxLength(50);
        builder.HasIndex(r => new { r.AdmissionId, r.AssessmentType, r.AssessedAt });

        builder.HasOne(r => r.Admission).WithMany().HasForeignKey(r => r.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.AssessedByUser).WithMany().HasForeignKey(r => r.AssessedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CarePlanConfiguration : IEntityTypeConfiguration<CarePlan>
{
    public void Configure(EntityTypeBuilder<CarePlan> builder)
    {
        builder.ToTable("care_plans", "nursing");
        builder.Property(c => c.ProblemStatement).HasMaxLength(500);
        builder.Property(c => c.Goal).HasMaxLength(500);
        builder.Property(c => c.Interventions).HasMaxLength(2000);
        builder.Property(c => c.EvaluationNotes).HasMaxLength(2000);
        builder.HasIndex(c => new { c.AdmissionId, c.Status });

        builder.HasOne(c => c.Admission).WithMany().HasForeignKey(c => c.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TransferNoteConfiguration : IEntityTypeConfiguration<TransferNote>
{
    public void Configure(EntityTypeBuilder<TransferNote> builder)
    {
        builder.ToTable("transfer_notes", "nursing");
        builder.Property(t => t.HandoverNotes).HasMaxLength(2000);
        builder.HasIndex(t => t.AdmissionId);

        builder.HasOne(t => t.Admission).WithMany().HasForeignKey(t => t.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.BedAllocation).WithMany().HasForeignKey(t => t.BedAllocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.TransferredByUser).WithMany().HasForeignKey(t => t.TransferredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ReceivedByUser).WithMany().HasForeignKey(t => t.ReceivedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MarScheduleConfiguration : IEntityTypeConfiguration<MarSchedule>
{
    public void Configure(EntityTypeBuilder<MarSchedule> builder)
    {
        builder.ToTable("mar_schedules", "nursing");
        builder.HasIndex(m => new { m.AdmissionId, m.DueAt });
        builder.HasIndex(m => m.MedicationOrderId);

        builder.HasOne(m => m.Admission).WithMany().HasForeignKey(m => m.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.MedicationOrder).WithMany().HasForeignKey(m => m.MedicationOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MarAdministrationConfiguration : IEntityTypeConfiguration<MarAdministration>
{
    public void Configure(EntityTypeBuilder<MarAdministration> builder)
    {
        builder.ToTable("mar_administrations", "nursing");
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.Notes).HasMaxLength(500);
        builder.Property(a => a.Quantity).HasPrecision(18, 3);
        builder.HasIndex(a => a.MarScheduleId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(a => a.MarSchedule).WithOne(m => m.Administration).HasForeignKey<MarAdministration>(a => a.MarScheduleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.ActionByUser).WithMany().HasForeignKey(a => a.ActionByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.VerifiedByUser).WithMany().HasForeignKey(a => a.VerifiedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Batch).WithMany().HasForeignKey(a => a.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShiftHandoverConfiguration : IEntityTypeConfiguration<ShiftHandover>
{
    public void Configure(EntityTypeBuilder<ShiftHandover> builder)
    {
        builder.ToTable("shift_handovers", "nursing");
        builder.Property(h => h.GeneralNotes).HasMaxLength(2000);
        builder.HasIndex(h => new { h.WardId, h.ShiftDate, h.Shift }).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(h => h.Ward).WithMany().HasForeignKey(h => h.WardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.HandoverFromUser).WithMany().HasForeignKey(h => h.HandoverFromUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.HandoverToUser).WithMany().HasForeignKey(h => h.HandoverToUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(h => h.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PatientHandoverNoteConfiguration : IEntityTypeConfiguration<PatientHandoverNote>
{
    public void Configure(EntityTypeBuilder<PatientHandoverNote> builder)
    {
        builder.ToTable("patient_handover_notes", "nursing");
        builder.Property(n => n.Situation).HasMaxLength(1000);
        builder.Property(n => n.Background).HasMaxLength(1000);
        builder.Property(n => n.Assessment).HasMaxLength(1000);
        builder.Property(n => n.Recommendation).HasMaxLength(1000);
        builder.HasIndex(n => n.ShiftHandoverId);
        builder.HasIndex(n => n.AdmissionId);

        builder.HasOne(n => n.ShiftHandover).WithMany(h => h.PatientNotes).HasForeignKey(n => n.ShiftHandoverId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(n => n.Admission).WithMany().HasForeignKey(n => n.AdmissionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents", "nursing");
        builder.Property(i => i.IncidentType).HasMaxLength(100);
        builder.Property(i => i.Location).HasMaxLength(200);
        builder.Property(i => i.Description).HasMaxLength(4000);
        builder.Property(i => i.ImmediateActionTaken).HasMaxLength(2000);
        builder.Property(i => i.ReviewNotes).HasMaxLength(2000);
        builder.Property(i => i.SourceModule).HasMaxLength(30);
        builder.Property(i => i.RootCauseAnalysis).HasMaxLength(4000);
        builder.HasIndex(i => new { i.BranchId, i.Status, i.IncidentDate });

        builder.HasOne(i => i.Patient).WithMany().HasForeignKey(i => i.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Admission).WithMany().HasForeignKey(i => i.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.ReportedByUser).WithMany().HasForeignKey(i => i.ReportedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.ReviewedByUser).WithMany().HasForeignKey(i => i.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(i => i.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
