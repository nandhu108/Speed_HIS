using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 9 IPD tables. Schema: ipd. Admission is branch-scoped; every child table inherits
// scoping through it. Money numeric(18,2), quantities numeric(18,3) per CLAUDE.md.

public class AdmissionConfiguration : IEntityTypeConfiguration<Admission>
{
    public void Configure(EntityTypeBuilder<Admission> builder)
    {
        builder.ToTable("admissions", "ipd");
        builder.Property(a => a.AdmissionNumber).HasMaxLength(30);
        builder.Property(a => a.CreditLimit).HasPrecision(18, 2);
        builder.Property(a => a.ProvisionalDiagnosis).HasMaxLength(500);
        builder.Property(a => a.DischargeTypeNotes).HasMaxLength(500);
        builder.Property(a => a.CancelReason).HasMaxLength(500);
        builder.HasIndex(a => new { a.BranchId, a.AdmissionNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(a => new { a.PatientId, a.Status });

        builder.HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.PrimaryConsultant).WithMany().HasForeignKey(a => a.PrimaryConsultantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Department).WithMany().HasForeignKey(a => a.DepartmentId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.ReferringOpVisit).WithMany().HasForeignKey(a => a.ReferringOpVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.ReferringEmergencyVisit).WithMany().HasForeignKey(a => a.ReferringEmergencyVisitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.CurrentBed).WithMany().HasForeignKey(a => a.CurrentBedId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.PatientConsent).WithMany().HasForeignKey(a => a.PatientConsentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.DischargeRequestedByDoctor).WithMany().HasForeignKey(a => a.DischargeRequestedByDoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AdmissionConsultantConfiguration : IEntityTypeConfiguration<AdmissionConsultant>
{
    public void Configure(EntityTypeBuilder<AdmissionConsultant> builder)
    {
        builder.ToTable("admission_consultants", "ipd");
        builder.Property(c => c.Notes).HasMaxLength(500);
        builder.HasIndex(c => c.AdmissionId);
        builder.HasOne(c => c.Admission).WithMany(a => a.Consultants).HasForeignKey(c => c.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Doctor).WithMany().HasForeignKey(c => c.DoctorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BedAllocationConfiguration : IEntityTypeConfiguration<BedAllocation>
{
    public void Configure(EntityTypeBuilder<BedAllocation> builder)
    {
        builder.ToTable("bed_allocations", "ipd");
        builder.Property(b => b.BedChargePerDay).HasPrecision(18, 2);
        builder.Property(b => b.TransferReason).HasMaxLength(500);
        builder.HasIndex(b => b.AdmissionId);
        // Concurrency safety: at most one active (ReleasedAt null) allocation per bed, and
        // per admission — a second concurrent allocation attempt hits this index and fails
        // instead of double-booking the bed.
        builder.HasIndex(b => b.BedId).IsUnique().HasFilter("released_at IS NULL AND is_deleted = false");
        builder.HasIndex(b => b.AdmissionId).IsUnique().HasFilter("released_at IS NULL AND is_deleted = false");

        builder.HasOne(b => b.Admission).WithMany(a => a.BedAllocations).HasForeignKey(b => b.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Bed).WithMany().HasForeignKey(b => b.BedId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DoctorOrderConfiguration : IEntityTypeConfiguration<DoctorOrder>
{
    public void Configure(EntityTypeBuilder<DoctorOrder> builder)
    {
        builder.ToTable("doctor_orders", "ipd");
        builder.Property(o => o.Description).HasMaxLength(1000);
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.CancelReason).HasMaxLength(500);
        builder.HasIndex(o => new { o.AdmissionId, o.OrderType });

        builder.HasOne(o => o.Admission).WithMany().HasForeignKey(o => o.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.OrderedByDoctor).WithMany().HasForeignKey(o => o.OrderedByDoctorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MedicationOrderConfiguration : IEntityTypeConfiguration<MedicationOrder>
{
    public void Configure(EntityTypeBuilder<MedicationOrder> builder)
    {
        builder.ToTable("medication_orders", "ipd");
        builder.Property(m => m.Dose).HasMaxLength(100);
        builder.Property(m => m.Route).HasMaxLength(50);
        builder.Property(m => m.Frequency).HasMaxLength(50);
        builder.Property(m => m.Instructions).HasMaxLength(500);
        builder.HasIndex(m => m.DoctorOrderId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(m => m.DoctorOrder).WithOne(o => o.MedicationOrder).HasForeignKey<MedicationOrder>(m => m.DoctorOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Drug).WithMany().HasForeignKey(m => m.DrugId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DietOrderConfiguration : IEntityTypeConfiguration<DietOrder>
{
    public void Configure(EntityTypeBuilder<DietOrder> builder)
    {
        builder.ToTable("diet_orders", "ipd");
        builder.Property(d => d.DietType).HasMaxLength(100);
        builder.Property(d => d.Instructions).HasMaxLength(500);
        builder.HasIndex(d => d.DoctorOrderId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(d => d.DoctorOrder).WithOne(o => o.DietOrder).HasForeignKey<DietOrder>(d => d.DoctorOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProgressNoteConfiguration : IEntityTypeConfiguration<ProgressNote>
{
    public void Configure(EntityTypeBuilder<ProgressNote> builder)
    {
        builder.ToTable("progress_notes", "ipd");
        builder.Property(n => n.Notes).HasMaxLength(4000);
        builder.HasIndex(n => n.AdmissionId);
        builder.HasOne(n => n.Admission).WithMany().HasForeignKey(n => n.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.Doctor).WithMany().HasForeignKey(n => n.DoctorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class IpVitalsConfiguration : IEntityTypeConfiguration<IpVitals>
{
    public void Configure(EntityTypeBuilder<IpVitals> builder)
    {
        builder.ToTable("ip_vitals", "ipd");
        builder.Property(v => v.TemperatureF).HasPrecision(5, 2);
        builder.Property(v => v.RandomBloodSugar).HasPrecision(6, 2);
        builder.Property(v => v.UrineOutputMl).HasPrecision(8, 2);
        builder.HasIndex(v => new { v.AdmissionId, v.RecordedAt });
        builder.HasOne(v => v.Admission).WithMany().HasForeignKey(v => v.AdmissionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class IntakeOutputEntryConfiguration : IEntityTypeConfiguration<IntakeOutputEntry>
{
    public void Configure(EntityTypeBuilder<IntakeOutputEntry> builder)
    {
        builder.ToTable("intake_output_entries", "ipd");
        builder.Property(e => e.Category).HasMaxLength(100);
        builder.Property(e => e.QuantityMl).HasPrecision(10, 2);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.HasIndex(e => new { e.AdmissionId, e.RecordedAt });
        builder.HasOne(e => e.Admission).WithMany().HasForeignKey(e => e.AdmissionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProcedureNoteConfiguration : IEntityTypeConfiguration<ProcedureNote>
{
    public void Configure(EntityTypeBuilder<ProcedureNote> builder)
    {
        builder.ToTable("procedure_notes", "ipd");
        builder.Property(n => n.ProcedureName).HasMaxLength(200);
        builder.Property(n => n.Findings).HasMaxLength(2000);
        builder.Property(n => n.Notes).HasMaxLength(2000);
        builder.HasIndex(n => n.AdmissionId);
        builder.HasOne(n => n.Admission).WithMany().HasForeignKey(n => n.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.PerformedByDoctor).WithMany().HasForeignKey(n => n.PerformedByDoctorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DischargeClearanceConfiguration : IEntityTypeConfiguration<DischargeClearance>
{
    public void Configure(EntityTypeBuilder<DischargeClearance> builder)
    {
        builder.ToTable("discharge_clearances", "ipd");
        builder.Property(c => c.Remarks).HasMaxLength(500);
        builder.HasIndex(c => new { c.AdmissionId, c.ClearanceType }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(c => c.Admission).WithMany(a => a.Clearances).HasForeignKey(c => c.AdmissionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DischargeSummaryConfiguration : IEntityTypeConfiguration<DischargeSummary>
{
    public void Configure(EntityTypeBuilder<DischargeSummary> builder)
    {
        builder.ToTable("discharge_summaries", "ipd");
        builder.Property(s => s.FinalDiagnosis).HasMaxLength(1000);
        builder.Property(s => s.CourseInHospital).HasMaxLength(4000);
        builder.Property(s => s.MedicationsOnDischarge).HasMaxLength(2000);
        builder.Property(s => s.FollowUpAdvice).HasMaxLength(2000);
        builder.Property(s => s.ConditionAtDischarge).HasMaxLength(200);
        builder.HasIndex(s => s.AdmissionId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(s => s.Admission).WithMany().HasForeignKey(s => s.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.ApprovedByDoctor).WithMany().HasForeignKey(s => s.ApprovedByDoctorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AdmissionRequestConfiguration : IEntityTypeConfiguration<AdmissionRequest>
{
    public void Configure(EntityTypeBuilder<AdmissionRequest> builder)
    {
        builder.ToTable("admission_requests", "ipd");
        builder.Property(r => r.Reason).HasMaxLength(500);
        builder.Property(r => r.RejectionReason).HasMaxLength(500);
        builder.HasIndex(r => new { r.BranchId, r.Status });

        builder.HasOne(r => r.Patient).WithMany().HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.RequestedConsultant).WithMany().HasForeignKey(r => r.RequestedConsultantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Ward).WithMany().HasForeignKey(r => r.WardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.RequestedByUser).WithMany().HasForeignKey(r => r.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.DecidedByUser).WithMany().HasForeignKey(r => r.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.ApprovedAdmission).WithMany().HasForeignKey(r => r.ApprovedAdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(r => r.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
