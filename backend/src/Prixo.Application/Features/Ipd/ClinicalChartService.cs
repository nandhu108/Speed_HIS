using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Ipd;

/// <summary>
/// Progress notes, vitals chart, intake/output chart and procedure notes (plan §9's
/// "Inpatient Clinical" block). Repeatable, admission-scoped charting — Phase 10 (Nursing)
/// deepens vitals/I-O with shift-tagging and risk scores; this is the basic version.
/// </summary>
public class ClinicalChartService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> AddProgressNoteAsync(long admissionId, AddProgressNoteRequest request, CancellationToken ct = default)
    {
        await RequireActiveAdmissionAsync(admissionId, ct);
        if (!await db.Doctors.AnyAsync(d => d.Id == request.DoctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), request.DoctorId);
        }
        await Prixo.Application.Common.Security.ClinicalActor.ResolveAttributedDoctorAsync(db, currentUser, request.DoctorId, ct);
        var note = new ProgressNote { AdmissionId = admissionId, DoctorId = request.DoctorId, NoteAt = timeProvider.GetUtcNow(), Notes = request.Notes };
        db.ProgressNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return note.Id;
    }

    public async Task<IReadOnlyList<ProgressNoteDto>> GetProgressNotesAsync(long admissionId, CancellationToken ct = default) =>
        await db.ProgressNotes.AsNoTracking().Where(n => n.AdmissionId == admissionId)
            .Include(n => n.Doctor).ThenInclude(d => d!.User)
            .OrderByDescending(n => n.NoteAt)
            .Select(n => new ProgressNoteDto(n.Id, n.DoctorId, n.Doctor!.User!.FullName, n.NoteAt, n.Notes))
            .ToListAsync(ct);

    public async Task<long> AddVitalsAsync(long admissionId, AddVitalsRequest request, CancellationToken ct = default)
    {
        await RequireActiveAdmissionAsync(admissionId, ct);
        var vitals = new IpVitals
        {
            AdmissionId = admissionId, RecordedAt = timeProvider.GetUtcNow(), RecordedByUserId = currentUser.UserId,
            TemperatureF = request.TemperatureF, PulseRate = request.PulseRate, RespiratoryRate = request.RespiratoryRate,
            BloodPressureSystolic = request.BloodPressureSystolic, BloodPressureDiastolic = request.BloodPressureDiastolic,
            SpO2 = request.SpO2, RandomBloodSugar = request.RandomBloodSugar,
            GlasgowComaScale = request.GlasgowComaScale, MeanArterialPressure = request.MeanArterialPressure,
            CentralVenousPressure = request.CentralVenousPressure, UrineOutputMl = request.UrineOutputMl,
        };
        db.IpVitals.Add(vitals);
        await db.SaveChangesAsync(ct);
        return vitals.Id;
    }

    public async Task<IReadOnlyList<IpVitalsDto>> GetVitalsAsync(long admissionId, CancellationToken ct = default) =>
        await db.IpVitals.AsNoTracking().Where(v => v.AdmissionId == admissionId)
            .OrderByDescending(v => v.RecordedAt)
            .Join(db.Users.AsNoTracking(), v => v.RecordedByUserId, u => u.Id, (v, u) => new IpVitalsDto(
                v.Id, v.RecordedAt, u.FullName, v.TemperatureF, v.PulseRate, v.RespiratoryRate,
                v.BloodPressureSystolic, v.BloodPressureDiastolic, v.SpO2, v.RandomBloodSugar,
                v.GlasgowComaScale, v.MeanArterialPressure, v.CentralVenousPressure, v.UrineOutputMl))
            .ToListAsync(ct);

    public async Task<long> AddIntakeOutputAsync(long admissionId, AddIntakeOutputRequest request, CancellationToken ct = default)
    {
        await RequireActiveAdmissionAsync(admissionId, ct);
        var entry = new IntakeOutputEntry
        {
            AdmissionId = admissionId, RecordedAt = timeProvider.GetUtcNow(), IsIntake = request.IsIntake,
            Category = request.Category, QuantityMl = request.QuantityMl, Notes = request.Notes,
        };
        db.IntakeOutputEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id;
    }

    public async Task<IReadOnlyList<IntakeOutputDto>> GetIntakeOutputAsync(long admissionId, CancellationToken ct = default) =>
        await db.IntakeOutputEntries.AsNoTracking().Where(e => e.AdmissionId == admissionId)
            .OrderByDescending(e => e.RecordedAt)
            .Select(e => new IntakeOutputDto(e.Id, e.RecordedAt, e.IsIntake, e.Category, e.QuantityMl, e.Notes))
            .ToListAsync(ct);

    public async Task<long> AddProcedureNoteAsync(long admissionId, AddProcedureNoteRequest request, CancellationToken ct = default)
    {
        await RequireActiveAdmissionAsync(admissionId, ct);
        if (!await db.Doctors.AnyAsync(d => d.Id == request.PerformedByDoctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), request.PerformedByDoctorId);
        }
        await Prixo.Application.Common.Security.ClinicalActor.ResolveAttributedDoctorAsync(db, currentUser, request.PerformedByDoctorId, ct);
        var note = new ProcedureNote
        {
            AdmissionId = admissionId, ProcedureName = request.ProcedureName, PerformedByDoctorId = request.PerformedByDoctorId,
            PerformedAt = request.PerformedAt, Findings = request.Findings, Notes = request.Notes,
        };
        db.ProcedureNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return note.Id;
    }

    public async Task<IReadOnlyList<ProcedureNoteDto>> GetProcedureNotesAsync(long admissionId, CancellationToken ct = default) =>
        await db.ProcedureNotes.AsNoTracking().Where(n => n.AdmissionId == admissionId)
            .Include(n => n.PerformedByDoctor).ThenInclude(d => d!.User)
            .OrderByDescending(n => n.PerformedAt)
            .Select(n => new ProcedureNoteDto(n.Id, n.ProcedureName, n.PerformedByDoctorId, n.PerformedByDoctor!.User!.FullName, n.PerformedAt, n.Findings, n.Notes))
            .ToListAsync(ct);

    private async Task RequireActiveAdmissionAsync(long admissionId, CancellationToken ct)
    {
        var status = await db.Admissions.AsNoTracking().Where(a => a.Id == admissionId).Select(a => (AdmissionStatus?)a.Status).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Admission), admissionId);
        if (status != AdmissionStatus.Admitted)
        {
            throw new DomainException("Clinical entries can only be recorded against an active admission.");
        }
    }
}
