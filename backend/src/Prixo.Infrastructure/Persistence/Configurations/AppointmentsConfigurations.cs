using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Appointments;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Infrastructure.Persistence.Configurations;

// Phase 4 appointment tables. Schema: appointment. Branch-scoped (a doctor's schedule
// and bookings are per branch they work at).

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("doctor_schedules", "appointment");
        builder.HasIndex(s => new { s.DoctorId, s.BranchId, s.DayOfWeek });
        builder.HasOne(s => s.Doctor).WithMany()
            .HasForeignKey(s => s.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ScheduleOverrideConfiguration : IEntityTypeConfiguration<ScheduleOverride>
{
    public void Configure(EntityTypeBuilder<ScheduleOverride> builder)
    {
        builder.ToTable("schedule_overrides", "appointment");
        builder.Property(o => o.Reason).HasMaxLength(300);
        builder.HasIndex(o => new { o.DoctorId, o.Date }).IsUnique().HasFilter("is_deleted = false");
        builder.HasOne(o => o.Doctor).WithMany()
            .HasForeignKey(o => o.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(o => o.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class UnitScheduleConfiguration : IEntityTypeConfiguration<UnitSchedule>
{
    public void Configure(EntityTypeBuilder<UnitSchedule> builder)
    {
        builder.ToTable("unit_schedules", "appointment");
        builder.HasIndex(s => new { s.UnitId, s.BranchId, s.DayOfWeek });
        builder.HasOne(s => s.Unit).WithMany()
            .HasForeignKey(s => s.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments", "appointment");
        builder.Property(a => a.Notes).HasMaxLength(500);
        builder.Property(a => a.CancelReason).HasMaxLength(500);
        builder.Property(a => a.TeleconsultLink).HasMaxLength(500);
        builder.Property(a => a.ConsultationMode).HasDefaultValue(ConsultationMode.InPerson);

        builder.HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.StartTime });
        builder.HasIndex(a => a.PatientId);
        builder.HasIndex(a => new { a.BranchId, a.AppointmentDate });

        builder.HasOne(a => a.Patient).WithMany()
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Doctor).WithMany()
            .HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.AppointmentType).WithMany()
            .HasForeignKey(a => a.AppointmentTypeId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.RescheduledFrom).WithMany()
            .HasForeignKey(a => a.RescheduledFromId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.TransferredFromDoctor).WithMany()
            .HasForeignKey(a => a.TransferredFromDoctorId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.EmergencyVisit).WithMany()
            .HasForeignKey(a => a.EmergencyVisitId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        // One active ("Scheduled"/"CheckedIn"/"Completed") token per ER visit — mirrors Billing's
        // unique-active-bill-per-emergency-visit pattern. AppointmentStatus.Cancelled = 4.
        builder.HasIndex(a => a.EmergencyVisitId)
            .IsUnique()
            .HasFilter("is_deleted = false AND emergency_visit_id IS NOT NULL AND status <> 4");
    }
}

public class NotificationQueueConfiguration : IEntityTypeConfiguration<NotificationQueue>
{
    public void Configure(EntityTypeBuilder<NotificationQueue> builder)
    {
        builder.ToTable("notification_queue", "admin");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Recipient).HasMaxLength(200);
        builder.Property(n => n.Subject).HasMaxLength(200);
        builder.Property(n => n.Body).HasMaxLength(1000);
        builder.Property(n => n.RelatedEntityType).HasMaxLength(100);
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => new { n.RelatedEntityType, n.RelatedEntityId });
    }
}
