using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Appointments;
using Prixo.Domain.Entities.Er;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Appointments;

public class AppointmentService(
    IAppDbContext db,
    ISequenceService sequences,
    ICurrentUser currentUser,
    IActivityLogger activity,
    IUnitOfWork unitOfWork,
    SettingService settings,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<SlotDto>> GetSlotsAsync(long doctorId, DateOnly date, CancellationToken ct = default)
    {
        var doctor = await db.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == doctorId, ct)
            ?? throw new NotFoundException(nameof(Doctor), doctorId);

        var windows = await BuildAvailabilityWindowsAsync(doctor, date, ct);
        if (windows.Count == 0)
        {
            return [];
        }

        var takenSlots = await db.Appointments.AsNoTracking()
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate == date && a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.StartTime)
            .ToListAsync(ct);
        var taken = takenSlots.ToHashSet();

        return windows
            .SelectMany(GenerateSlots)
            .Select(slot => slot with { IsAvailable = !taken.Contains(slot.StartTime) })
            .OrderBy(s => s.StartTime)
            .ToList();
    }

    public async Task<PagedResult<AppointmentListItemDto>> GetPagedAsync(AppointmentQuery query, CancellationToken ct = default)
    {
        var appointments = db.Appointments.AsNoTracking();

        if (query.DoctorId is { } doctorId) appointments = appointments.Where(a => a.DoctorId == doctorId);
        if (query.PatientId is { } patientId) appointments = appointments.Where(a => a.PatientId == patientId);
        if (query.Date is { } date) appointments = appointments.Where(a => a.AppointmentDate == date);
        if (query.Status is { } status) appointments = appointments.Where(a => a.Status == status);
        if (query.EmergencyVisitId is { } emergencyVisitId) appointments = appointments.Where(a => a.EmergencyVisitId == emergencyVisitId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            appointments = appointments.Where(a =>
                EF.Functions.ILike(a.Patient!.Uhid, term) ||
                EF.Functions.ILike(a.Patient!.FirstName, term) ||
                EF.Functions.ILike(a.Patient!.LastName, term));
        }

        var totalCount = await appointments.CountAsync(ct);
        var items = await appointments
            .OrderByDescending(a => a.AppointmentDate).ThenBy(a => a.StartTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AppointmentListItemDto(
                a.Id, a.PatientId, a.Patient!.Uhid, a.Patient!.FirstName + " " + a.Patient!.LastName,
                a.DoctorId, a.Doctor!.User!.FullName, a.AppointmentDate, a.StartTime, a.EndTime,
                a.TokenNumber, a.Status, a.BookingType,
                a.AppointmentTypeId, a.AppointmentType != null ? a.AppointmentType.Name : null,
                a.SkippedAt, a.EmergencyVisitId, a.ConsultationMode, a.TeleconsultLink))
            .ToListAsync(ct);

        return PagedResult<AppointmentListItemDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<AppointmentDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var appointment = await db.Appointments.AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .Include(a => a.AppointmentType)
            .Include(a => a.TransferredFromDoctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException(nameof(Appointment), id);

        return ToDetailDto(appointment);
    }

    public async Task<AppointmentDetailDto> BookAsync(BookAppointmentRequest request, CancellationToken ct = default)
    {
        var branchId = currentUser.BranchId ?? throw new DomainException("Select a branch before booking an appointment.");
        await EnsureWithinBookingWindowAsync(request.AppointmentDate, ct);
        var appointment = await BookInternalAsync(request, rescheduledFromId: null, branchId, ct);
        return ToDetailDto(appointment);
    }

    /// <summary>"Front Office control panel" — how far ahead staff may book, via
    /// FrontOffice.MaxAdvanceBookingDays (0 = unlimited). Deliberately scoped to this one staff
    /// booking action only — reschedule, ER token mapping, and Portal booking all call
    /// BookInternalAsync directly and are unaffected.</summary>
    private async Task EnsureWithinBookingWindowAsync(DateOnly appointmentDate, CancellationToken ct)
    {
        var maxDays = await settings.GetIntAsync(SettingKeys.FrontOfficeMaxAdvanceBookingDays, 0, ct);
        if (maxDays <= 0)
        {
            return;
        }
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (appointmentDate > today.AddDays(maxDays))
        {
            throw new DomainException($"Appointments can only be booked up to {maxDays} days in advance.");
        }
    }

    /// <summary>Portal booking (plan §21) — same booking engine, but the branch comes from the patient's choice rather than a staff-selected currentUser.BranchId (the portal JWT audience carries no branch claim).</summary>
    public async Task<AppointmentDetailDto> BookForPortalAsync(BookAppointmentRequest request, long branchId, CancellationToken ct = default)
    {
        var appointment = await BookInternalAsync(request, rescheduledFromId: null, branchId, ct);
        return ToDetailDto(appointment);
    }

    /// <summary>Cancels the old appointment and books a replacement in one transaction.</summary>
    public async Task<AppointmentDetailDto> RescheduleAsync(
        long id, RescheduleAppointmentRequest request, CancellationToken ct = default) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var old = await db.Appointments.FirstOrDefaultAsync(a => a.Id == id, token)
                ?? throw new NotFoundException(nameof(Appointment), id);
            if (old.Status != AppointmentStatus.Scheduled)
            {
                throw new DomainException("Only a scheduled appointment can be rescheduled.");
            }

            old.Status = AppointmentStatus.Cancelled;
            old.CancelReason = $"Rescheduled: {request.Reason}";
            old.CancelledAt = timeProvider.GetUtcNow();

            var replacement = await BookInternalAsync(
                new BookAppointmentRequest(old.PatientId, old.DoctorId, request.NewDate, request.NewStartTime,
                    AppointmentBookingType.Booked, old.Notes, old.AppointmentTypeId,
                    old.ConsultationMode, old.TeleconsultLink),
                rescheduledFromId: old.Id, old.BranchId, token);

            return ToDetailDto(replacement);
        }, ct);

    public async Task CancelAsync(long id, CancelAppointmentRequest request, CancellationToken ct = default)
    {
        var appointment = await db.Appointments.Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException(nameof(Appointment), id);
        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed or AppointmentStatus.NoShow)
        {
            throw new DomainException($"An appointment that is {appointment.Status} cannot be cancelled.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelReason = request.Reason;
        appointment.CancelledAt = timeProvider.GetUtcNow();

        await activity.LogAsync(ActivityEvents.AppointmentCancelled,
            $"Appointment for '{appointment.Patient!.FirstName} {appointment.Patient!.LastName}' " +
            $"on {appointment.AppointmentDate:yyyy-MM-dd} {appointment.StartTime} cancelled: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task CheckInAsync(long id, CancellationToken ct = default)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException(nameof(Appointment), id);
        if (appointment.Status != AppointmentStatus.Scheduled)
        {
            throw new DomainException($"An appointment that is {appointment.Status} cannot be checked in.");
        }

        appointment.Status = AppointmentStatus.CheckedIn;
        appointment.CheckedInAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>"Skip Token" — patient not present when called. Stays CheckedIn but sorts to the
    /// back of the queue (see GetQueueAsync) until recalled.</summary>
    public async Task SkipAsync(long id, CancellationToken ct = default)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException(nameof(Appointment), id);
        if (appointment.Status != AppointmentStatus.CheckedIn)
        {
            throw new DomainException($"An appointment that is {appointment.Status} cannot be skipped.");
        }

        appointment.SkippedAt = timeProvider.GetUtcNow();
        appointment.SkipCount++;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Restores a skipped token to normal queue rotation.</summary>
    public async Task RecallAsync(long id, CancellationToken ct = default)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException(nameof(Appointment), id);
        if (appointment.SkippedAt is null)
        {
            throw new DomainException("This token has not been skipped.");
        }

        appointment.SkippedAt = null;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>"Tele-consulting" — set/replace the meeting link once one actually exists; separate
    /// from booking since the link is often generated after the slot is reserved.</summary>
    public async Task SetTeleconsultLinkAsync(long id, SaveTeleconsultLinkRequest request, CancellationToken ct = default)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException(nameof(Appointment), id);
        if (appointment.ConsultationMode != ConsultationMode.TeleConsult)
        {
            throw new DomainException("Only a tele-consult appointment can have a meeting link.");
        }

        appointment.TeleconsultLink = request.TeleconsultLink;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>"Token Transfer" — reassigns the token to a different doctor the same day, minting
    /// a fresh token number from that doctor's own per-day sequence.</summary>
    public async Task<AppointmentDetailDto> TransferAsync(long id, TransferTokenRequest request, CancellationToken ct = default)
    {
        var appointment = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.AppointmentType)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException(nameof(Appointment), id);
        if (appointment.Status is not (AppointmentStatus.Scheduled or AppointmentStatus.CheckedIn))
        {
            throw new DomainException($"An appointment that is {appointment.Status} cannot be transferred.");
        }
        if (request.NewDoctorId == appointment.DoctorId)
        {
            throw new DomainException("Choose a different doctor to transfer this token to.");
        }

        var newDoctor = await db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == request.NewDoctorId, ct)
            ?? throw new NotFoundException(nameof(Doctor), request.NewDoctorId);

        var alreadyBooked = await db.Appointments.AnyAsync(a =>
            a.Id != id && a.DoctorId == request.NewDoctorId && a.AppointmentDate == appointment.AppointmentDate
            && a.StartTime == appointment.StartTime && a.Status != AppointmentStatus.Cancelled, ct);
        if (alreadyBooked)
        {
            throw new DomainException("The new doctor already has a booking at that time. Reschedule instead.");
        }

        var oldDoctor = appointment.Doctor!;
        var tokenScope = EncodeDate(appointment.AppointmentDate);
        var newTokenNumber = (int)await sequences.NextValueAsync(SequenceKeys.AppointmentToken, request.NewDoctorId, tokenScope, ct);
        var slotMinutes = await ResolveSlotMinutesAsync(newDoctor, appointment.AppointmentDate, appointment.StartTime, ct);

        appointment.TransferredFromDoctorId = oldDoctor.Id;
        appointment.TransferredFromDoctor = oldDoctor;
        appointment.TransferredAt = timeProvider.GetUtcNow();
        appointment.DoctorId = request.NewDoctorId;
        appointment.Doctor = newDoctor;
        appointment.TokenNumber = newTokenNumber;
        appointment.EndTime = appointment.StartTime.Add(TimeSpan.FromMinutes(slotMinutes));
        appointment.SkippedAt = null;

        await activity.LogAsync(ActivityEvents.AppointmentTransferred,
            $"Appointment for '{appointment.Patient!.FirstName} {appointment.Patient!.LastName}' " +
            $"transferred from Dr. {oldDoctor.User!.FullName} to Dr. {newDoctor.User!.FullName} " +
            $"on {appointment.AppointmentDate:yyyy-MM-dd} — new token #{newTokenNumber}" +
            (string.IsNullOrWhiteSpace(request.Reason) ? "" : $": {request.Reason}"), ct: ct);
        await db.SaveChangesAsync(ct);

        return ToDetailDto(appointment);
    }

    /// <summary>"ER Token Mapping" — once an ER visit has an assigned doctor, mints a normal walk-in
    /// token for that doctor via the same booking engine as any other walk-in, so the ER patient
    /// gets a real position in the doctor's OPD token queue/display board. Reuses AppointmentBook
    /// (no new Emergency permission needed), same posture as Billing's GenerateEmergencyBillAsync
    /// reusing Billing's Emergency-specific create permission.</summary>
    public async Task<AppointmentDetailDto> MapEmergencyTokenAsync(long emergencyVisitId, CancellationToken ct = default)
    {
        var visit = await db.EmergencyVisits.FirstOrDefaultAsync(v => v.Id == emergencyVisitId, ct)
            ?? throw new NotFoundException(nameof(EmergencyVisit), emergencyVisitId);
        if (visit.AssignedDoctorId is not { } doctorId)
        {
            throw new DomainException("Assign a doctor to the ER visit before mapping a token.");
        }
        if (await db.Appointments.AnyAsync(a => a.EmergencyVisitId == emergencyVisitId && a.Status != AppointmentStatus.Cancelled, ct))
        {
            throw new DomainException("A token has already been mapped for this ER visit.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var startTime = new TimeOnly(now.Hour, now.Minute);

        var appointment = await BookInternalAsync(
            new BookAppointmentRequest(visit.PatientId, doctorId, today, startTime, AppointmentBookingType.WalkIn,
                $"ER token mapping — visit {visit.VisitNumber}", null),
            rescheduledFromId: null, visit.BranchId, ct);
        appointment.EmergencyVisitId = emergencyVisitId;
        await db.SaveChangesAsync(ct);

        return ToDetailDto(appointment);
    }

    public async Task<IReadOnlyList<QueueEntryDto>> GetQueueAsync(
        long? doctorId, long? departmentId, DateOnly date, CancellationToken ct = default)
    {
        var appointments = db.Appointments.AsNoTracking()
            .Where(a => a.AppointmentDate == date
                && (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.CheckedIn));

        if (doctorId is { } id) appointments = appointments.Where(a => a.DoctorId == id);
        if (departmentId is { } deptId)
        {
            appointments = appointments.Where(a => a.Doctor!.DoctorDepartments.Any(dd => dd.DepartmentId == deptId));
        }

        // Skipped tokens (called, patient absent) sort to the back of their doctor's queue in
        // skip order rather than being removed; SkippedAt is null for every row still in normal
        // rotation, so the ThenBy(SkippedAt) tie-break only matters within the skipped group.
        return await appointments
            .OrderBy(a => a.DoctorId)
            .ThenBy(a => a.SkippedAt.HasValue)
            .ThenBy(a => a.SkippedAt)
            .ThenBy(a => a.TokenNumber)
            .Select(a => new QueueEntryDto(
                a.Id, a.TokenNumber, a.PatientId, a.Patient!.FirstName + " " + a.Patient!.LastName, a.Patient!.Uhid,
                a.DoctorId, a.Doctor!.User!.FullName, a.StartTime, a.Status, a.SkippedAt, a.EmergencyVisitId,
                a.ConsultationMode))
            .ToListAsync(ct);
    }

    /// <summary>"Doctors On Duty" — a pure projection of the existing per-doctor availability
    /// windows (the same BuildAvailabilityWindowsAsync the slot engine and booking already use)
    /// across every active doctor for a date; no new duty-roster schema.</summary>
    public async Task<IReadOnlyList<DoctorOnDutyDto>> GetDoctorsOnDutyAsync(
        DateOnly date, long? departmentId, CancellationToken ct = default)
    {
        var doctorsQuery = db.Doctors.AsNoTracking()
            .Include(d => d.User)
            .Include(d => d.Specialty)
            .Include(d => d.DoctorDepartments).ThenInclude(dd => dd.Department)
            .Where(d => d.IsActive);
        if (departmentId is { } deptId)
        {
            doctorsQuery = doctorsQuery.Where(d => d.DoctorDepartments.Any(dd => dd.DepartmentId == deptId));
        }
        var doctors = await doctorsQuery.OrderBy(d => d.User!.FullName).ToListAsync(ct);

        var result = new List<DoctorOnDutyDto>(doctors.Count);
        foreach (var doctor in doctors)
        {
            var windows = await BuildAvailabilityWindowsAsync(doctor, date, ct);
            string? leaveReason = null;
            if (windows.Count == 0)
            {
                var overrideRow = await db.ScheduleOverrides.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.DoctorId == doctor.Id && o.Date == date && !o.IsAvailable, ct);
                leaveReason = overrideRow?.Reason;
            }

            result.Add(new DoctorOnDutyDto(
                doctor.Id, doctor.User!.FullName, doctor.Specialty?.Name,
                doctor.DoctorDepartments.Select(dd => dd.Department!.Name).ToList(),
                windows.Count > 0,
                windows.Select(w => new DoctorOnDutyWindowDto(w.Start, w.End)).ToList(),
                leaveReason));
        }
        return result;
    }

    private async Task<Appointment> BookInternalAsync(
        BookAppointmentRequest request, long? rescheduledFromId, long branchId, CancellationToken ct)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, ct)
            ?? throw new NotFoundException(nameof(Patient), request.PatientId);
        var doctor = await db.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == request.DoctorId, ct)
            ?? throw new NotFoundException(nameof(Doctor), request.DoctorId);
        AppointmentType? appointmentType = null;
        if (request.AppointmentTypeId is { } appointmentTypeId)
        {
            appointmentType = await db.AppointmentTypes.FirstOrDefaultAsync(t => t.Id == appointmentTypeId, ct)
                ?? throw new NotFoundException(nameof(AppointmentType), appointmentTypeId);
        }

        var alreadyBooked = await db.Appointments.AnyAsync(a =>
            a.DoctorId == request.DoctorId && a.AppointmentDate == request.AppointmentDate
            && a.StartTime == request.StartTime && a.Status != AppointmentStatus.Cancelled, ct);
        if (alreadyBooked)
        {
            throw new DomainException("That slot has just been booked. Please choose another.");
        }

        var slotMinutes = await ResolveSlotMinutesAsync(doctor, request.AppointmentDate, request.StartTime, ct);
        var endTime = request.StartTime.Add(TimeSpan.FromMinutes(slotMinutes));

        var tokenScope = EncodeDate(request.AppointmentDate);
        var tokenNumber = (int)await sequences.NextValueAsync(SequenceKeys.AppointmentToken, request.DoctorId, tokenScope, ct);

        var appointment = new Appointment
        {
            BranchId = branchId,
            PatientId = request.PatientId,
            Patient = patient,
            DoctorId = request.DoctorId,
            Doctor = doctor,
            AppointmentDate = request.AppointmentDate,
            StartTime = request.StartTime,
            EndTime = endTime,
            TokenNumber = tokenNumber,
            BookingType = request.BookingType,
            AppointmentTypeId = request.AppointmentTypeId,
            AppointmentType = appointmentType,
            Notes = request.Notes,
            RescheduledFromId = rescheduledFromId,
            ConsultationMode = request.ConsultationMode,
            TeleconsultLink = request.ConsultationMode == ConsultationMode.TeleConsult ? request.TeleconsultLink : null,
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(ct); // assigns appointment.Id, needed for the notification's RelatedEntityId

        if (!string.IsNullOrWhiteSpace(patient.Mobile))
        {
            db.NotificationQueue.Add(new NotificationQueue
            {
                Channel = NotificationChannel.Sms,
                Recipient = patient.Mobile,
                Body = $"Reminder: your appointment with Dr. {doctor.User?.FullName} is on " +
                       $"{request.AppointmentDate:dd-MMM-yyyy} at {request.StartTime:HH:mm}. Token #{tokenNumber}.",
                RelatedEntityType = nameof(Appointment),
                RelatedEntityId = appointment.Id,
                CreatedAt = timeProvider.GetUtcNow(),
            });
            await db.SaveChangesAsync(ct);
        }

        return appointment;
    }

    private async Task<int> ResolveSlotMinutesAsync(Doctor doctor, DateOnly date, TimeOnly startTime, CancellationToken ct)
    {
        var windows = await BuildAvailabilityWindowsAsync(doctor, date, ct);
        var matching = windows.FirstOrDefault(w => startTime >= w.Start && startTime < w.End);
        return matching?.SlotMinutes ?? doctor.DefaultSlotMinutes;
    }

    private async Task<List<AvailabilityWindow>> BuildAvailabilityWindowsAsync(Doctor doctor, DateOnly date, CancellationToken ct)
    {
        var overrideRow = await db.ScheduleOverrides.AsNoTracking()
            .FirstOrDefaultAsync(o => o.DoctorId == doctor.Id && o.Date == date, ct);

        if (overrideRow is not null)
        {
            if (!overrideRow.IsAvailable)
            {
                return [];
            }
            return
            [
                new AvailabilityWindow(
                    overrideRow.StartTime!.Value, overrideRow.EndTime!.Value,
                    overrideRow.SlotMinutes ?? doctor.DefaultSlotMinutes),
            ];
        }

        var blocks = await db.DoctorSchedules.AsNoTracking()
            .Where(s => s.DoctorId == doctor.Id && s.DayOfWeek == date.DayOfWeek)
            .ToListAsync(ct);
        return blocks.Select(b => new AvailabilityWindow(b.StartTime, b.EndTime, b.SlotMinutes)).ToList();
    }

    private static IEnumerable<SlotDto> GenerateSlots(AvailabilityWindow window)
    {
        var current = window.Start;
        while (current.Add(TimeSpan.FromMinutes(window.SlotMinutes)) <= window.End)
        {
            var end = current.Add(TimeSpan.FromMinutes(window.SlotMinutes));
            yield return new SlotDto(current, end, true);
            current = end;
        }
    }

    private static int EncodeDate(DateOnly date) => (date.Year * 10000) + (date.Month * 100) + date.Day;

    private static AppointmentDetailDto ToDetailDto(Appointment a) => new(
        a.Id, a.PatientId, a.Patient!.Uhid, $"{a.Patient!.FirstName} {a.Patient!.LastName}", a.Patient!.Mobile,
        a.DoctorId, a.Doctor!.User!.FullName, a.AppointmentDate, a.StartTime, a.EndTime,
        a.TokenNumber, a.Status, a.BookingType, a.Notes,
        a.CancelReason, a.CancelledAt, a.CheckedInAt, a.NoShowMarkedAt, a.RescheduledFromId,
        a.AppointmentTypeId, a.AppointmentType?.Name,
        a.SkippedAt, a.SkipCount,
        a.TransferredFromDoctorId, a.TransferredFromDoctor?.User?.FullName, a.TransferredAt,
        a.EmergencyVisitId,
        a.ConsultationMode, a.TeleconsultLink);

    private sealed record AvailabilityWindow(TimeOnly Start, TimeOnly End, int SlotMinutes);
}
