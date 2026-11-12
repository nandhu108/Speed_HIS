using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Ot;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Ot;

/// <summary>
/// The WHO checklist's three phases plus the four clinical note templates (pre-anaesthesia,
/// intra-op, operative, post-op) — every one is a 1:1-per-booking record. The checklist row
/// always exists (BookingService seeds it at booking creation); the four note types are
/// find-or-create ("Save" upserts) since a booking may go a while before anyone fills them in.
/// </summary>
public class OtClinicalService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<ChecklistDto> GetChecklistAsync(long bookingId, CancellationToken ct = default)
    {
        var c = await db.SurgicalSafetyChecklists.AsNoTracking().FirstOrDefaultAsync(x => x.BookingId == bookingId, ct)
            ?? throw new NotFoundException(nameof(SurgicalSafetyChecklist), bookingId);
        return ToDto(c);
    }

    private static List<string> Missing(params (string Name, bool Ticked)[] items) =>
        items.Where(i => !i.Ticked).Select(i => i.Name).ToList();

    public async Task<ChecklistDto> UpdateSignInAsync(long bookingId, UpdateSignInRequest request, CancellationToken ct = default)
    {
        var c = await RequireChecklistAsync(bookingId, ct);
        c.PatientIdentityConfirmed = request.PatientIdentityConfirmed;
        c.SiteMarked = request.SiteMarked;
        c.AnaesthesiaSafetyCheckComplete = request.AnaesthesiaSafetyCheckComplete;
        c.PulseOximeterFunctioning = request.PulseOximeterFunctioning;
        c.KnownAllergy = request.KnownAllergy;
        c.DifficultAirwayRisk = request.DifficultAirwayRisk;
        c.RiskOfBloodLoss = request.RiskOfBloodLoss;
        // WHO Sign In: identity, site marking, anaesthesia safety check and a working pulse oximeter
        // are mandatory (allergy/airway/blood-loss are risk declarations, not ticks).
        var missing = Missing(("patient identity confirmed", c.PatientIdentityConfirmed), ("site marked", c.SiteMarked),
            ("anaesthesia safety check complete", c.AnaesthesiaSafetyCheckComplete), ("pulse oximeter functioning", c.PulseOximeterFunctioning));
        if (request.Complete && missing.Count > 0)
        {
            throw new DomainException($"Sign In cannot be completed: {string.Join(", ", missing)} not confirmed.");
        }
        if (missing.Count > 0)
        {
            c.SignInCompletedAt = null;
            c.SignInCompletedByUserId = null;
        }
        else if (request.Complete && c.SignInCompletedAt is null)
        {
            c.SignInCompletedAt = timeProvider.GetUtcNow();
            c.SignInCompletedByUserId = currentUser.UserId;
        }
        await db.SaveChangesAsync(ct);
        return ToDto(c);
    }

    public async Task<ChecklistDto> UpdateTimeOutAsync(long bookingId, UpdateTimeOutRequest request, CancellationToken ct = default)
    {
        var c = await RequireChecklistAsync(bookingId, ct);
        c.TeamIntroduced = request.TeamIntroduced;
        c.PatientSiteProcedureConfirmed = request.PatientSiteProcedureConfirmed;
        c.CriticalEventsReviewed = request.CriticalEventsReviewed;
        c.AntibioticProphylaxisGiven = request.AntibioticProphylaxisGiven;
        c.ImagingDisplayed = request.ImagingDisplayed;
        // Antibiotic prophylaxis and imaging display are "if applicable" items in the WHO list.
        var missing = Missing(("team introduced", c.TeamIntroduced), ("patient, site and procedure confirmed", c.PatientSiteProcedureConfirmed),
            ("critical events reviewed", c.CriticalEventsReviewed));
        if (request.Complete && missing.Count > 0)
        {
            throw new DomainException($"Time Out cannot be completed: {string.Join(", ", missing)} not confirmed.");
        }
        if (missing.Count > 0)
        {
            c.TimeOutCompletedAt = null;
            c.TimeOutCompletedByUserId = null;
        }
        else if (request.Complete && c.TimeOutCompletedAt is null)
        {
            c.TimeOutCompletedAt = timeProvider.GetUtcNow();
            c.TimeOutCompletedByUserId = currentUser.UserId;
        }
        await db.SaveChangesAsync(ct);
        return ToDto(c);
    }

    public async Task<ChecklistDto> UpdateSignOutAsync(long bookingId, UpdateSignOutRequest request, CancellationToken ct = default)
    {
        var c = await RequireChecklistAsync(bookingId, ct);
        c.ProcedureNameRecorded = request.ProcedureNameRecorded;
        c.InstrumentSpongeNeedleCountCorrect = request.InstrumentSpongeNeedleCountCorrect;
        c.SpecimenLabeled = request.SpecimenLabeled;
        c.EquipmentProblemsAddressed = request.EquipmentProblemsAddressed;
        c.RecoveryConcernsReviewed = request.RecoveryConcernsReviewed;
        // Specimen labelling is "if any"; the count, procedure record, equipment and recovery
        // review are mandatory before the patient leaves theatre.
        var missing = Missing(("procedure name recorded", c.ProcedureNameRecorded),
            ("instrument, sponge and needle count correct", c.InstrumentSpongeNeedleCountCorrect),
            ("equipment problems addressed", c.EquipmentProblemsAddressed), ("recovery concerns reviewed", c.RecoveryConcernsReviewed));
        if (request.Complete && missing.Count > 0)
        {
            throw new DomainException($"Sign Out cannot be completed: {string.Join(", ", missing)} not confirmed.");
        }
        if (missing.Count > 0)
        {
            c.SignOutCompletedAt = null;
            c.SignOutCompletedByUserId = null;
        }
        else if (request.Complete && c.SignOutCompletedAt is null)
        {
            c.SignOutCompletedAt = timeProvider.GetUtcNow();
            c.SignOutCompletedByUserId = currentUser.UserId;
        }
        await db.SaveChangesAsync(ct);
        return ToDto(c);
    }

    public async Task<PreAnaesthesiaDto> SavePreAnaesthesiaAsync(long bookingId, SavePreAnaesthesiaRequest request, CancellationToken ct = default)
    {
        await RequireBookingAsync(bookingId, ct);
        if (!await db.Doctors.AnyAsync(d => d.Id == request.AssessedByDoctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), request.AssessedByDoctorId);
        }

        var a = await db.PreAnaesthesiaAssessments.FirstOrDefaultAsync(x => x.BookingId == bookingId, ct);
        if (a is null)
        {
            a = new PreAnaesthesiaAssessment { BookingId = bookingId, AsaGrade = request.AsaGrade, AssessedAt = timeProvider.GetUtcNow() };
            db.PreAnaesthesiaAssessments.Add(a);
        }
        a.AssessedByDoctorId = request.AssessedByDoctorId;
        a.AsaGrade = request.AsaGrade;
        a.AirwayAssessment = request.AirwayAssessment;
        a.Comorbidities = request.Comorbidities;
        a.Investigations = request.Investigations;
        a.AnaesthesiaPlan = request.AnaesthesiaPlan;
        a.Notes = request.Notes;
        await db.SaveChangesAsync(ct);

        var doctorName = await db.Doctors.Where(d => d.Id == a.AssessedByDoctorId).Select(d => d.User!.FullName).FirstAsync(ct);
        return new PreAnaesthesiaDto(a.Id, a.BookingId, a.AssessedByDoctorId, doctorName, a.AssessedAt, a.AsaGrade,
            a.AirwayAssessment, a.Comorbidities, a.Investigations, a.AnaesthesiaPlan, a.Notes);
    }

    public async Task<PreAnaesthesiaDto> GetPreAnaesthesiaAsync(long bookingId, CancellationToken ct = default)
    {
        var a = await db.PreAnaesthesiaAssessments.AsNoTracking().Include(x => x.AssessedByDoctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(x => x.BookingId == bookingId, ct) ?? throw new NotFoundException(nameof(PreAnaesthesiaAssessment), bookingId);
        return new PreAnaesthesiaDto(a.Id, a.BookingId, a.AssessedByDoctorId, a.AssessedByDoctor!.User!.FullName, a.AssessedAt, a.AsaGrade,
            a.AirwayAssessment, a.Comorbidities, a.Investigations, a.AnaesthesiaPlan, a.Notes);
    }

    public async Task<IntraOpDto> SaveIntraOpAsync(long bookingId, SaveIntraOpRequest request, CancellationToken ct = default)
    {
        await RequireBookingAsync(bookingId, ct);
        if (!await db.Doctors.AnyAsync(d => d.Id == request.AdministeredByDoctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), request.AdministeredByDoctorId);
        }

        var r = await db.IntraOpAnaesthesiaRecords.FirstOrDefaultAsync(x => x.BookingId == bookingId, ct);
        if (r is null)
        {
            r = new IntraOpAnaesthesiaRecord { BookingId = bookingId };
            db.IntraOpAnaesthesiaRecords.Add(r);
        }
        r.AnaesthesiaType = request.AnaesthesiaType;
        r.AdministeredByDoctorId = request.AdministeredByDoctorId;
        r.StartedAt = request.StartedAt;
        r.EndedAt = request.EndedAt;
        r.Notes = request.Notes;
        await db.SaveChangesAsync(ct);

        var doctorName = await db.Doctors.Where(d => d.Id == r.AdministeredByDoctorId).Select(d => d.User!.FullName).FirstAsync(ct);
        return new IntraOpDto(r.Id, r.BookingId, r.AnaesthesiaType, r.AdministeredByDoctorId, doctorName, r.StartedAt, r.EndedAt, r.Notes);
    }

    public async Task<IntraOpDto> GetIntraOpAsync(long bookingId, CancellationToken ct = default)
    {
        var r = await db.IntraOpAnaesthesiaRecords.AsNoTracking().Include(x => x.AdministeredByDoctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(x => x.BookingId == bookingId, ct) ?? throw new NotFoundException(nameof(IntraOpAnaesthesiaRecord), bookingId);
        return new IntraOpDto(r.Id, r.BookingId, r.AnaesthesiaType, r.AdministeredByDoctorId, r.AdministeredByDoctor!.User!.FullName, r.StartedAt, r.EndedAt, r.Notes);
    }

    public async Task<OperativeNoteDto> SaveOperativeNoteAsync(long bookingId, SaveOperativeNoteRequest request, CancellationToken ct = default)
    {
        await RequireBookingAsync(bookingId, ct);
        if (!await db.Doctors.AnyAsync(d => d.Id == request.SurgeonId, ct))
        {
            throw new NotFoundException(nameof(Doctor), request.SurgeonId);
        }

        var n = await db.OperativeNotes.FirstOrDefaultAsync(x => x.BookingId == bookingId, ct);
        if (n is null)
        {
            n = new OperativeNote { BookingId = bookingId, ProcedurePerformed = request.ProcedurePerformed, StartedAt = request.StartedAt };
            db.OperativeNotes.Add(n);
        }
        n.SurgeonId = request.SurgeonId;
        n.ProcedurePerformed = request.ProcedurePerformed;
        n.Findings = request.Findings;
        n.Complications = request.Complications;
        n.StartedAt = request.StartedAt;
        n.EndedAt = request.EndedAt;
        n.Notes = request.Notes;
        await db.SaveChangesAsync(ct);

        var surgeonName = await db.Doctors.Where(d => d.Id == n.SurgeonId).Select(d => d.User!.FullName).FirstAsync(ct);
        return new OperativeNoteDto(n.Id, n.BookingId, n.SurgeonId, surgeonName, n.ProcedurePerformed, n.Findings, n.Complications, n.StartedAt, n.EndedAt, n.Notes);
    }

    public async Task<OperativeNoteDto> GetOperativeNoteAsync(long bookingId, CancellationToken ct = default)
    {
        var n = await db.OperativeNotes.AsNoTracking().Include(x => x.Surgeon).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(x => x.BookingId == bookingId, ct) ?? throw new NotFoundException(nameof(OperativeNote), bookingId);
        return new OperativeNoteDto(n.Id, n.BookingId, n.SurgeonId, n.Surgeon!.User!.FullName, n.ProcedurePerformed, n.Findings, n.Complications, n.StartedAt, n.EndedAt, n.Notes);
    }

    public async Task<PostOpNoteDto> SavePostOpNoteAsync(long bookingId, SavePostOpNoteRequest request, CancellationToken ct = default)
    {
        await RequireBookingAsync(bookingId, ct);
        var n = await db.PostOpNotes.FirstOrDefaultAsync(x => x.BookingId == bookingId, ct);
        if (n is null)
        {
            n = new PostOpNote { BookingId = bookingId };
            db.PostOpNotes.Add(n);
        }
        n.RecoveryNotes = request.RecoveryNotes;
        n.VitalsSummary = request.VitalsSummary;
        n.Complications = request.Complications;
        n.ShiftedAt = request.ShiftedAt;
        n.Notes = request.Notes;
        await db.SaveChangesAsync(ct);
        return new PostOpNoteDto(n.Id, n.BookingId, n.RecoveryNotes, n.VitalsSummary, n.Complications, n.ShiftedAt, n.Notes);
    }

    public async Task<PostOpNoteDto> GetPostOpNoteAsync(long bookingId, CancellationToken ct = default)
    {
        var n = await db.PostOpNotes.AsNoTracking().FirstOrDefaultAsync(x => x.BookingId == bookingId, ct)
            ?? throw new NotFoundException(nameof(PostOpNote), bookingId);
        return new PostOpNoteDto(n.Id, n.BookingId, n.RecoveryNotes, n.VitalsSummary, n.Complications, n.ShiftedAt, n.Notes);
    }

    private async Task RequireBookingAsync(long bookingId, CancellationToken ct)
    {
        if (!await db.SurgeryBookings.AnyAsync(b => b.Id == bookingId, ct))
        {
            throw new NotFoundException(nameof(SurgeryBooking), bookingId);
        }
    }

    private async Task<SurgicalSafetyChecklist> RequireChecklistAsync(long bookingId, CancellationToken ct) =>
        await db.SurgicalSafetyChecklists.FirstOrDefaultAsync(x => x.BookingId == bookingId, ct)
            ?? throw new NotFoundException(nameof(SurgicalSafetyChecklist), bookingId);

    private static ChecklistDto ToDto(SurgicalSafetyChecklist c) => new(
        c.BookingId,
        c.PatientIdentityConfirmed, c.SiteMarked, c.AnaesthesiaSafetyCheckComplete, c.PulseOximeterFunctioning,
        c.KnownAllergy, c.DifficultAirwayRisk, c.RiskOfBloodLoss, c.SignInCompletedAt,
        c.TeamIntroduced, c.PatientSiteProcedureConfirmed, c.CriticalEventsReviewed,
        c.AntibioticProphylaxisGiven, c.ImagingDisplayed, c.TimeOutCompletedAt,
        c.ProcedureNameRecorded, c.InstrumentSpongeNeedleCountCorrect, c.SpecimenLabeled,
        c.EquipmentProblemsAddressed, c.RecoveryConcernsReviewed, c.SignOutCompletedAt);
}
