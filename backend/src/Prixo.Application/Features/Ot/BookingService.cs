using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Features.Admin.Settings;
using Prixo.Application.Features.Billing;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Ot;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Ot;

/// <summary>
/// Surgery booking lifecycle: create (with a Theatre/Surgeon/Anaesthetist conflict check),
/// reschedule, cancel/postpone, and the Scheduled→InProgress→Completed transitions —
/// gated on the WHO checklist's Sign In (before InProgress) and Sign Out (before
/// Completed) phases plus a verified consent, same "checklist gates the next step" posture
/// as IPD's DischargeClearance. Creates the SurgicalSafetyChecklist row eagerly alongside
/// the booking (like StockAudit seeds its Items) so there's always exactly one row to
/// update in place.
/// </summary>
public class BookingService(
    IAppDbContext db, ISequenceService sequences, ICurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider,
    SettingService settings, IUnitOfWork unitOfWork)
{
    public async Task<long> CreateAsync(CreateBookingRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        var admission = await db.Admissions.FirstOrDefaultAsync(a => a.Id == request.AdmissionId, ct)
            ?? throw new NotFoundException(nameof(Admission), request.AdmissionId);
        if (!await db.Theatres.AnyAsync(t => t.Id == request.TheatreId, ct))
        {
            throw new NotFoundException(nameof(Theatre), request.TheatreId);
        }
        if (!await db.Doctors.AnyAsync(d => d.Id == request.SurgeonId, ct))
        {
            throw new NotFoundException(nameof(Doctor), request.SurgeonId);
        }
        if (request.AnaesthetistId is { } anaesthetistId && !await db.Doctors.AnyAsync(d => d.Id == anaesthetistId, ct))
        {
            throw new NotFoundException(nameof(Doctor), anaesthetistId);
        }
        if (request.ScheduledEnd <= request.ScheduledStart)
        {
            throw new DomainException("Scheduled end must be after scheduled start.");
        }
        if (request.ScheduledStart < timeProvider.GetUtcNow().AddMinutes(-15))
        {
            throw new DomainException("A surgery cannot be booked in the past.");
        }
        if (admission.Status != AdmissionStatus.Admitted)
        {
            throw new DomainException($"An admission that is {admission.Status} cannot be booked for surgery.");
        }
        if (request.AnaesthetistId == request.SurgeonId)
        {
            throw new DomainException("The surgeon and the anaesthetist must be different doctors.");
        }

        await EnsureNoConflictAsync(request.TheatreId, request.SurgeonId, request.AnaesthetistId, request.ScheduledStart, request.ScheduledEnd, null, ct);

        var today = Today();
        var number = await sequences.NextValueAsync(SequenceKeys.SurgeryBookingNumber, branchId, today.Year, ct);
        var booking = new SurgeryBooking
        {
            BranchId = branchId,
            PatientId = admission.PatientId,
            AdmissionId = admission.Id,
            TheatreId = request.TheatreId,
            BookingNumber = $"OT-{today.Year}-{number.ToString().PadLeft(6, '0')}",
            ProcedureName = request.ProcedureName,
            SurgeonId = request.SurgeonId,
            AnaesthetistId = request.AnaesthetistId,
            ScheduledStart = request.ScheduledStart,
            ScheduledEnd = request.ScheduledEnd,
            Notes = request.Notes,
        };
        db.SurgeryBookings.Add(booking);
        await db.SaveChangesAsync(ct);

        db.SurgicalSafetyChecklists.Add(new SurgicalSafetyChecklist { BookingId = booking.Id });
        await activity.LogAsync(ActivityEvents.SurgeryBookingCreated, $"Surgery {booking.BookingNumber} booked: {booking.ProcedureName}", ct: ct);
        await db.SaveChangesAsync(ct);
        return booking.Id;
    }

    public async Task RescheduleAsync(long id, RescheduleBookingRequest request, CancellationToken ct = default)
    {
        var booking = await db.SurgeryBookings.FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw new NotFoundException(nameof(SurgeryBooking), id);
        if (booking.Status != SurgeryStatus.Scheduled)
        {
            throw new DomainException($"A booking that is {booking.Status} cannot be rescheduled.");
        }
        if (request.ScheduledEnd <= request.ScheduledStart)
        {
            throw new DomainException("Scheduled end must be after scheduled start.");
        }
        if (request.ScheduledStart < timeProvider.GetUtcNow().AddMinutes(-15))
        {
            throw new DomainException("A surgery cannot be rescheduled into the past.");
        }
        await EnsureNoConflictAsync(booking.TheatreId, booking.SurgeonId, booking.AnaesthetistId, request.ScheduledStart, request.ScheduledEnd, booking.Id, ct);

        booking.ScheduledStart = request.ScheduledStart;
        booking.ScheduledEnd = request.ScheduledEnd;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>"OT Patient Update" — re-points a Scheduled booking to a different admission
    /// (and thus patient) when the wrong one was picked at booking time. Same status gate as
    /// reschedule — once consent verification/conduct has started, this is no longer a simple
    /// data-entry fix.</summary>
    public async Task UpdatePatientAsync(long id, UpdateBookingPatientRequest request, CancellationToken ct = default)
    {
        var booking = await db.SurgeryBookings.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException(nameof(SurgeryBooking), id);
        if (booking.Status != SurgeryStatus.Scheduled)
        {
            throw new DomainException($"A booking that is {booking.Status} cannot have its patient changed.");
        }
        var admission = await db.Admissions.FirstOrDefaultAsync(a => a.Id == request.AdmissionId, ct)
            ?? throw new NotFoundException(nameof(Admission), request.AdmissionId);

        booking.AdmissionId = admission.Id;
        booking.PatientId = admission.PatientId;
        await db.SaveChangesAsync(ct);
    }

    public async Task VerifyConsentAsync(long id, VerifyConsentRequest request, CancellationToken ct = default)
    {
        var booking = await db.SurgeryBookings.FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw new NotFoundException(nameof(SurgeryBooking), id);
        var consent = await db.PatientConsents.FirstOrDefaultAsync(c => c.Id == request.PatientConsentId && c.PatientId == booking.PatientId, ct)
            ?? throw new NotFoundException(nameof(PatientConsent), request.PatientConsentId);

        booking.PatientConsentId = consent.Id;
        booking.ConsentVerifiedAt = timeProvider.GetUtcNow();
        booking.ConsentVerifiedByUserId = currentUser.UserId;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Scheduled → InProgress. Gated on a verified consent and the checklist's Sign In phase.</summary>
    public async Task StartAsync(long id, CancellationToken ct = default)
    {
        var booking = await db.SurgeryBookings.FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw new NotFoundException(nameof(SurgeryBooking), id);
        if (booking.Status != SurgeryStatus.Scheduled)
        {
            throw new DomainException($"A booking that is {booking.Status} cannot be started.");
        }
        if (booking.ConsentVerifiedAt is null)
        {
            throw new DomainException("Verify the surgical consent before starting the case.");
        }
        var checklist = await db.SurgicalSafetyChecklists.FirstOrDefaultAsync(c => c.BookingId == id, ct)
            ?? throw new NotFoundException(nameof(SurgicalSafetyChecklist), id);
        if (checklist.SignInCompletedAt is null)
        {
            throw new DomainException("Complete the Sign In checklist before starting the case.");
        }

        booking.Status = SurgeryStatus.InProgress;
        booking.ActualStart = timeProvider.GetUtcNow();
        await activity.LogAsync(ActivityEvents.SurgeryStarted, $"Surgery {booking.BookingNumber} started", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>InProgress → Completed. Gated on the checklist's Sign Out phase.</summary>
    public async Task CompleteAsync(long id, CancellationToken ct = default)
    {
        var booking = await db.SurgeryBookings.FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw new NotFoundException(nameof(SurgeryBooking), id);
        if (booking.Status != SurgeryStatus.InProgress)
        {
            throw new DomainException($"A booking that is {booking.Status} cannot be completed.");
        }
        var checklist = await db.SurgicalSafetyChecklists.FirstOrDefaultAsync(c => c.BookingId == id, ct)
            ?? throw new NotFoundException(nameof(SurgicalSafetyChecklist), id);
        if (checklist.SignOutCompletedAt is null)
        {
            throw new DomainException("Complete the Sign Out checklist before closing the case.");
        }

        booking.Status = SurgeryStatus.Completed;
        booking.ActualEnd = timeProvider.GetUtcNow();
        await activity.LogAsync(ActivityEvents.SurgeryCompleted, $"Surgery {booking.BookingNumber} completed", ct: ct);

        // Theatre time: one unit of the configured OT-hour service per started hour, on the IP bill.
        var serviceCode = (await settings.GetStringAsync(SettingKeys.OtHourlyChargeServiceCode, "", ct)).Trim();
        if (serviceCode.Length > 0 && booking.ActualStart is { } started)
        {
            var service = await db.Services.Include(s => s.Tax).FirstOrDefaultAsync(s => s.Code == serviceCode, ct);
            if (service is not null)
            {
                var hours = Math.Max(1, (int)Math.Ceiling((booking.ActualEnd.Value - started).TotalHours));
                var today = DateOnly.FromDateTime(booking.ActualEnd.Value.UtcDateTime);
                var bill = await IpBillAccess.GetOrCreateOpenAsync(db, sequences, booking.AdmissionId, today, ct);
                var patientCategoryId = await db.Patients.AsNoTracking().Where(p => p.Id == booking.PatientId).Select(p => p.PatientCategoryId).FirstOrDefaultAsync(ct);
                var rate = await IpBillAccess.ResolveServiceRateAsync(db, bill.BranchId, service, patientCategoryId, today, null, ct);
                var tax = service.IsTaxable ? (service.Tax?.RatePercent ?? 0) : 0;
                IpBillAccess.AddLine(bill, BillItemSourceType.OtCharge, $"{service.Name} — {booking.BookingNumber} ({hours} h)", hours, rate, tax, service.Id);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(long id, CancelBookingRequest request, CancellationToken ct = default)
    {
        var booking = await db.SurgeryBookings.FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw new NotFoundException(nameof(SurgeryBooking), id);
        if (booking.Status is not (SurgeryStatus.Scheduled or SurgeryStatus.Postponed))
        {
            throw new DomainException($"A booking that is {booking.Status} cannot be cancelled.");
        }
        booking.Status = SurgeryStatus.Cancelled;
        booking.StatusReason = request.Reason;
        await activity.LogAsync(ActivityEvents.SurgeryBookingCancelled, $"Surgery {booking.BookingNumber} cancelled: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task PostponeAsync(long id, PostponeBookingRequest request, CancellationToken ct = default)
    {
        var booking = await db.SurgeryBookings.FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw new NotFoundException(nameof(SurgeryBooking), id);
        if (booking.Status != SurgeryStatus.Scheduled)
        {
            throw new DomainException($"A booking that is {booking.Status} cannot be postponed.");
        }
        booking.Status = SurgeryStatus.Postponed;
        booking.StatusReason = request.Reason;
        await db.SaveChangesAsync(ct);
    }

    public async Task<long> AddTeamMemberAsync(long id, AddTeamMemberRequest request, CancellationToken ct = default)
    {
        if (!await db.SurgeryBookings.AnyAsync(b => b.Id == id, ct))
        {
            throw new NotFoundException(nameof(SurgeryBooking), id);
        }
        if (request.DoctorId is { } doctorId && !await db.Doctors.AnyAsync(d => d.Id == doctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), doctorId);
        }
        var member = new SurgeryTeamMember { BookingId = id, Role = request.Role, DoctorId = request.DoctorId, StaffName = request.StaffName };
        db.SurgeryTeamMembers.Add(member);
        await db.SaveChangesAsync(ct);
        return member.Id;
    }

    public async Task RemoveTeamMemberAsync(long id, long memberId, CancellationToken ct = default)
    {
        var member = await db.SurgeryTeamMembers.FirstOrDefaultAsync(m => m.Id == memberId && m.BookingId == id, ct)
            ?? throw new NotFoundException(nameof(SurgeryTeamMember), memberId);
        db.SurgeryTeamMembers.Remove(member);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<BookingListItemDto>> GetPagedAsync(BookingQuery query, CancellationToken ct = default)
    {
        var bookings = db.SurgeryBookings.AsNoTracking();
        if (query.Status is { } status) bookings = bookings.Where(b => b.Status == status);
        if (query.TheatreId is { } theatreId) bookings = bookings.Where(b => b.TheatreId == theatreId);
        if (query.FromDate is { } from)
        {
            var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            bookings = bookings.Where(b => b.ScheduledStart >= start);
        }
        if (query.ToDate is { } to)
        {
            var end = new DateTimeOffset(to.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1);
            bookings = bookings.Where(b => b.ScheduledStart < end);
        }

        return await bookings.Include(b => b.Patient).Include(b => b.Theatre)
            .Include(b => b.Surgeon).ThenInclude(d => d!.User)
            .Include(b => b.Anaesthetist).ThenInclude(d => d!.User)
            .OrderBy(b => b.ScheduledStart)
            .Select(b => new BookingListItemDto(
                b.Id, b.BookingNumber, b.Patient!.FirstName + " " + b.Patient.LastName, b.Theatre!.Name, b.ProcedureName,
                b.Surgeon!.User!.FullName, b.Anaesthetist != null ? b.Anaesthetist.User!.FullName : null,
                b.ScheduledStart, b.ScheduledEnd, b.Status))
            .ToListAsync(ct);
    }

    public async Task<BookingDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var b = await db.SurgeryBookings.AsNoTracking()
            .Include(x => x.Patient).Include(x => x.Admission).Include(x => x.Theatre)
            .Include(x => x.Surgeon).ThenInclude(d => d!.User)
            .Include(x => x.Anaesthetist).ThenInclude(d => d!.User)
            .Include(x => x.TeamMembers).ThenInclude(m => m.Doctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(SurgeryBooking), id);

        return new BookingDetailDto(
            b.Id, b.BookingNumber, b.PatientId, b.Patient!.FirstName + " " + b.Patient.LastName, b.AdmissionId, b.Admission!.AdmissionNumber,
            b.TheatreId, b.Theatre!.Name, b.ProcedureName,
            b.SurgeonId, b.Surgeon!.User!.FullName, b.AnaesthetistId, b.Anaesthetist != null ? b.Anaesthetist.User!.FullName : null,
            b.ScheduledStart, b.ScheduledEnd, b.ActualStart, b.ActualEnd,
            b.Status, b.StatusReason, b.PatientConsentId, b.ConsentVerifiedAt, b.Notes,
            b.TeamMembers.Select(m => new SurgeryTeamMemberDto(
                m.Id, m.Role, m.DoctorId, m.Doctor != null ? m.Doctor.User!.FullName : null, m.StaffName)).ToList());
    }

    /// <summary>Statutory OT register — every booking in the date range regardless of status, most recent first.</summary>
    public async Task<IReadOnlyList<OtRegisterRowDto>> GetRegisterAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        var start = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1);

        return await db.SurgeryBookings.AsNoTracking()
            .Where(b => b.ScheduledStart >= start && b.ScheduledStart < end)
            .Include(b => b.Patient).Include(b => b.Theatre)
            .Include(b => b.Surgeon).ThenInclude(d => d!.User)
            .Include(b => b.Anaesthetist).ThenInclude(d => d!.User)
            .OrderByDescending(b => b.ScheduledStart)
            .Select(b => new OtRegisterRowDto(
                b.Id, b.BookingNumber, b.Patient!.FirstName + " " + b.Patient.LastName, b.ProcedureName, b.Theatre!.Name,
                b.Surgeon!.User!.FullName, b.Anaesthetist != null ? b.Anaesthetist.User!.FullName : null,
                b.ScheduledStart, b.ActualStart, b.ActualEnd, b.Status))
            .ToListAsync(ct);
    }

    private async Task EnsureNoConflictAsync(
        long theatreId, long surgeonId, long? anaesthetistId, DateTimeOffset start, DateTimeOffset end, long? excludeBookingId, CancellationToken ct)
    {
        var candidates = await db.SurgeryBookings.AsNoTracking()
            .Where(b => b.Id != excludeBookingId
                && b.Status != SurgeryStatus.Cancelled && b.Status != SurgeryStatus.Postponed
                && (b.TheatreId == theatreId || b.SurgeonId == surgeonId || (anaesthetistId != null && b.AnaesthetistId == anaesthetistId)))
            .Select(b => new { b.TheatreId, b.SurgeonId, b.AnaesthetistId, b.ScheduledStart, b.ScheduledEnd, b.BookingNumber })
            .ToListAsync(ct);

        foreach (var candidate in candidates)
        {
            if (!OtMath.Overlaps(start, end, candidate.ScheduledStart, candidate.ScheduledEnd))
            {
                continue;
            }
            if (candidate.TheatreId == theatreId)
            {
                throw new DomainException($"Theatre is already booked by {candidate.BookingNumber} in that window.");
            }
            if (candidate.SurgeonId == surgeonId)
            {
                throw new DomainException($"Surgeon is already booked on {candidate.BookingNumber} in that window.");
            }
            if (anaesthetistId != null && candidate.AnaesthetistId == anaesthetistId)
            {
                throw new DomainException($"Anaesthetist is already booked on {candidate.BookingNumber} in that window.");
            }
        }
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
