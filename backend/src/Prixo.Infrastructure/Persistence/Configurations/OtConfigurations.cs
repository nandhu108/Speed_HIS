using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ot;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 14 OT tables. Schema: ot. SurgeryBooking is branch-scoped; every child table
// inherits scoping through it. Money numeric(18,2), quantities numeric(18,3) per CLAUDE.md.

public class TheatreConfiguration : IEntityTypeConfiguration<Theatre>
{
    public void Configure(EntityTypeBuilder<Theatre> builder)
    {
        builder.ToTable("theatres", "ot");
        builder.Property(t => t.Code).HasMaxLength(30);
        builder.Property(t => t.Name).HasMaxLength(150);
        builder.HasIndex(t => new { t.BranchId, t.Code }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne<Branch>().WithMany().HasForeignKey(t => t.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("equipment", "ot");
        builder.Property(e => e.Code).HasMaxLength(30);
        builder.Property(e => e.Name).HasMaxLength(150);
        builder.HasIndex(e => new { e.BranchId, e.Code }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(e => e.Theatre).WithMany().HasForeignKey(e => e.TheatreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SurgeryBookingConfiguration : IEntityTypeConfiguration<SurgeryBooking>
{
    public void Configure(EntityTypeBuilder<SurgeryBooking> builder)
    {
        builder.ToTable("surgery_bookings", "ot");
        builder.Property(b => b.BookingNumber).HasMaxLength(30);
        builder.Property(b => b.ProcedureName).HasMaxLength(300);
        builder.Property(b => b.StatusReason).HasMaxLength(500);
        builder.Property(b => b.Notes).HasMaxLength(1000);
        builder.HasIndex(b => new { b.BranchId, b.BookingNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(b => new { b.TheatreId, b.ScheduledStart });
        builder.HasIndex(b => new { b.SurgeonId, b.ScheduledStart });
        builder.HasIndex(b => new { b.AnaesthetistId, b.ScheduledStart });

        builder.HasOne(b => b.Patient).WithMany().HasForeignKey(b => b.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Admission).WithMany().HasForeignKey(b => b.AdmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Theatre).WithMany().HasForeignKey(b => b.TheatreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Surgeon).WithMany().HasForeignKey(b => b.SurgeonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Anaesthetist).WithMany().HasForeignKey(b => b.AnaesthetistId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.PatientConsent).WithMany().HasForeignKey(b => b.PatientConsentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.ConsentVerifiedByUser).WithMany().HasForeignKey(b => b.ConsentVerifiedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(b => b.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SurgeryTeamMemberConfiguration : IEntityTypeConfiguration<SurgeryTeamMember>
{
    public void Configure(EntityTypeBuilder<SurgeryTeamMember> builder)
    {
        builder.ToTable("surgery_team_members", "ot");
        builder.Property(m => m.StaffName).HasMaxLength(150);
        builder.HasIndex(m => m.BookingId);
        builder.HasOne(m => m.Booking).WithMany(b => b.TeamMembers).HasForeignKey(m => m.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Doctor).WithMany().HasForeignKey(m => m.DoctorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SurgicalSafetyChecklistConfiguration : IEntityTypeConfiguration<SurgicalSafetyChecklist>
{
    public void Configure(EntityTypeBuilder<SurgicalSafetyChecklist> builder)
    {
        builder.ToTable("surgical_safety_checklists", "ot");
        builder.Property(c => c.Notes).HasMaxLength(1000);
        builder.HasIndex(c => c.BookingId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(c => c.Booking).WithMany().HasForeignKey(c => c.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.SignInCompletedByUser).WithMany().HasForeignKey(c => c.SignInCompletedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.TimeOutCompletedByUser).WithMany().HasForeignKey(c => c.TimeOutCompletedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.SignOutCompletedByUser).WithMany().HasForeignKey(c => c.SignOutCompletedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PreAnaesthesiaAssessmentConfiguration : IEntityTypeConfiguration<PreAnaesthesiaAssessment>
{
    public void Configure(EntityTypeBuilder<PreAnaesthesiaAssessment> builder)
    {
        builder.ToTable("pre_anaesthesia_assessments", "ot");
        builder.Property(a => a.AsaGrade).HasMaxLength(20);
        builder.Property(a => a.AirwayAssessment).HasMaxLength(500);
        builder.Property(a => a.Comorbidities).HasMaxLength(1000);
        builder.Property(a => a.Investigations).HasMaxLength(1000);
        builder.Property(a => a.AnaesthesiaPlan).HasMaxLength(500);
        builder.Property(a => a.Notes).HasMaxLength(1000);
        builder.HasIndex(a => a.BookingId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(a => a.Booking).WithMany().HasForeignKey(a => a.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.AssessedByDoctor).WithMany().HasForeignKey(a => a.AssessedByDoctorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class IntraOpAnaesthesiaRecordConfiguration : IEntityTypeConfiguration<IntraOpAnaesthesiaRecord>
{
    public void Configure(EntityTypeBuilder<IntraOpAnaesthesiaRecord> builder)
    {
        builder.ToTable("intra_op_anaesthesia_records", "ot");
        builder.Property(r => r.Notes).HasMaxLength(1000);
        builder.HasIndex(r => r.BookingId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(r => r.Booking).WithMany().HasForeignKey(r => r.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.AdministeredByDoctor).WithMany().HasForeignKey(r => r.AdministeredByDoctorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class OperativeNoteConfiguration : IEntityTypeConfiguration<OperativeNote>
{
    public void Configure(EntityTypeBuilder<OperativeNote> builder)
    {
        builder.ToTable("operative_notes", "ot");
        builder.Property(n => n.ProcedurePerformed).HasMaxLength(300);
        builder.Property(n => n.Findings).HasMaxLength(2000);
        builder.Property(n => n.Complications).HasMaxLength(1000);
        builder.Property(n => n.Notes).HasMaxLength(1000);
        builder.HasIndex(n => n.BookingId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(n => n.Booking).WithMany().HasForeignKey(n => n.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.Surgeon).WithMany().HasForeignKey(n => n.SurgeonId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PostOpNoteConfiguration : IEntityTypeConfiguration<PostOpNote>
{
    public void Configure(EntityTypeBuilder<PostOpNote> builder)
    {
        builder.ToTable("post_op_notes", "ot");
        builder.Property(n => n.RecoveryNotes).HasMaxLength(2000);
        builder.Property(n => n.VitalsSummary).HasMaxLength(500);
        builder.Property(n => n.Complications).HasMaxLength(1000);
        builder.Property(n => n.Notes).HasMaxLength(1000);
        builder.HasIndex(n => n.BookingId).IsUnique().HasFilter("is_deleted = false");

        builder.HasOne(n => n.Booking).WithMany().HasForeignKey(n => n.BookingId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImplantUsageConfiguration : IEntityTypeConfiguration<ImplantUsage>
{
    public void Configure(EntityTypeBuilder<ImplantUsage> builder)
    {
        builder.ToTable("implant_usages", "ot");
        builder.Property(u => u.SerialNumber).HasMaxLength(100);
        builder.Property(u => u.Quantity).HasPrecision(18, 3);
        builder.Property(u => u.Rate).HasPrecision(18, 2);
        builder.HasIndex(u => u.BookingId);

        builder.HasOne(u => u.Booking).WithMany().HasForeignKey(u => u.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(u => u.Batch).WithMany().HasForeignKey(u => u.BatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(u => u.BillItem).WithMany().HasForeignKey(u => u.BillItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ConsumableUsageConfiguration : IEntityTypeConfiguration<ConsumableUsage>
{
    public void Configure(EntityTypeBuilder<ConsumableUsage> builder)
    {
        builder.ToTable("consumable_usages", "ot");
        builder.Property(u => u.Quantity).HasPrecision(18, 3);
        builder.Property(u => u.Rate).HasPrecision(18, 2);
        builder.HasIndex(u => u.BookingId);

        builder.HasOne(u => u.Booking).WithMany().HasForeignKey(u => u.BookingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(u => u.Batch).WithMany().HasForeignKey(u => u.BatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(u => u.BillItem).WithMany().HasForeignKey(u => u.BillItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
