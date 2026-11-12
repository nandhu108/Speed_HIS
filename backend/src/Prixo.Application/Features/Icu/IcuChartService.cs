using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Icu;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Icu;

/// <summary>
/// Ventilator and infusion charts (each a session + periodic flowsheet rows) and the
/// clinician-assigned severity assessment (plan §15's "Vitals Monitoring... Ventilator
/// Chart... Infusion Chart" and "ICU Dashboard... severity"). Session start/stop mirrors
/// IPD's ClinicalChartService admission-must-be-active guard.
/// </summary>
public class IcuChartService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    // ── Ventilator ───────────────────────────────────────────────────────────
    public async Task<long> StartVentilatorSessionAsync(long admissionId, StartVentilatorSessionRequest request, CancellationToken ct = default)
    {
        await RequireActiveAdmissionAsync(admissionId, ct);
        if (await db.VentilatorSessions.AnyAsync(s => s.AdmissionId == admissionId && s.StoppedAt == null, ct))
        {
            throw new DomainException("This admission already has an active ventilator session.");
        }
        var session = new VentilatorSession
        {
            AdmissionId = admissionId, StartedAt = timeProvider.GetUtcNow(), StartedByUserId = currentUser.UserId, Notes = request.Notes,
        };
        db.VentilatorSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session.Id;
    }

    public async Task StopVentilatorSessionAsync(long sessionId, CancellationToken ct = default)
    {
        var session = await db.VentilatorSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new NotFoundException(nameof(VentilatorSession), sessionId);
        if (session.StoppedAt is not null)
        {
            throw new DomainException("This ventilator session is already stopped.");
        }
        session.StoppedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task<long> AddVentilatorObservationAsync(long sessionId, AddVentilatorObservationRequest request, CancellationToken ct = default)
    {
        var session = await db.VentilatorSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new NotFoundException(nameof(VentilatorSession), sessionId);
        if (session.StoppedAt is not null)
        {
            throw new DomainException("Cannot chart against a stopped ventilator session.");
        }
        var observation = new VentilatorObservation
        {
            SessionId = sessionId, RecordedAt = timeProvider.GetUtcNow(), RecordedByUserId = currentUser.UserId,
            Mode = request.Mode, FiO2Percent = request.FiO2Percent, Peep = request.Peep,
            TidalVolumeMl = request.TidalVolumeMl, RespiratoryRateSet = request.RespiratoryRateSet,
            PeakPressure = request.PeakPressure, PlateauPressure = request.PlateauPressure, Notes = request.Notes,
        };
        db.VentilatorObservations.Add(observation);
        await db.SaveChangesAsync(ct);
        return observation.Id;
    }

    public async Task<IReadOnlyList<VentilatorSessionDto>> GetVentilatorSessionsAsync(long admissionId, CancellationToken ct = default)
    {
        var sessions = await db.VentilatorSessions.AsNoTracking().Where(s => s.AdmissionId == admissionId)
            .Include(s => s.StartedByUser)
            .Include(s => s.Observations).ThenInclude(o => o.RecordedByUser)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(ct);

        return sessions.Select(s => new VentilatorSessionDto(
            s.Id, s.AdmissionId, s.StartedAt, s.StartedByUser!.FullName, s.StoppedAt, s.Notes,
            s.Observations.OrderByDescending(o => o.RecordedAt).Select(o => new VentilatorObservationDto(
                o.Id, o.RecordedAt, o.RecordedByUser!.FullName, o.Mode, o.FiO2Percent, o.Peep,
                o.TidalVolumeMl, o.RespiratoryRateSet, o.PeakPressure, o.PlateauPressure, o.Notes)).ToList())).ToList();
    }

    // ── Infusion ─────────────────────────────────────────────────────────────
    public async Task<long> StartInfusionAsync(long admissionId, StartInfusionRequest request, CancellationToken ct = default)
    {
        await RequireActiveAdmissionAsync(admissionId, ct);
        if (request.DrugId is { } drugId && !await db.Drugs.AnyAsync(d => d.Id == drugId, ct))
        {
            throw new NotFoundException(nameof(Drug), drugId);
        }
        if (request.DrugId is null && string.IsNullOrWhiteSpace(request.DrugNameOverride))
        {
            throw new DomainException("Specify a catalog drug or a fluid name.");
        }
        var entry = new InfusionChartEntry
        {
            AdmissionId = admissionId, DrugId = request.DrugId, DrugNameOverride = request.DrugNameOverride,
            StartedAt = timeProvider.GetUtcNow(), StartedByUserId = currentUser.UserId, Notes = request.Notes,
        };
        db.InfusionChartEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id;
    }

    public async Task StopInfusionAsync(long entryId, CancellationToken ct = default)
    {
        var entry = await db.InfusionChartEntries.FirstOrDefaultAsync(e => e.Id == entryId, ct)
            ?? throw new NotFoundException(nameof(InfusionChartEntry), entryId);
        if (entry.StoppedAt is not null)
        {
            throw new DomainException("This infusion is already stopped.");
        }
        entry.StoppedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task<long> AddInfusionRateAsync(long entryId, AddInfusionRateRequest request, CancellationToken ct = default)
    {
        var entry = await db.InfusionChartEntries.FirstOrDefaultAsync(e => e.Id == entryId, ct)
            ?? throw new NotFoundException(nameof(InfusionChartEntry), entryId);
        if (entry.StoppedAt is not null)
        {
            throw new DomainException("Cannot chart against a stopped infusion.");
        }
        var rate = new InfusionRateEntry
        {
            InfusionChartEntryId = entryId, RecordedAt = timeProvider.GetUtcNow(), RecordedByUserId = currentUser.UserId,
            Rate = request.Rate, RateUnit = request.RateUnit, Notes = request.Notes,
        };
        db.InfusionRateEntries.Add(rate);
        await db.SaveChangesAsync(ct);
        return rate.Id;
    }

    public async Task<IReadOnlyList<InfusionChartEntryDto>> GetInfusionsAsync(long admissionId, CancellationToken ct = default)
    {
        var entries = await db.InfusionChartEntries.AsNoTracking().Where(e => e.AdmissionId == admissionId)
            .Include(e => e.Drug).Include(e => e.StartedByUser)
            .Include(e => e.RateEntries).ThenInclude(r => r.RecordedByUser)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync(ct);

        return entries.Select(e => new InfusionChartEntryDto(
            e.Id, e.AdmissionId, e.DrugId, e.Drug?.Name ?? e.DrugNameOverride ?? "", e.StartedAt, e.StartedByUser!.FullName, e.StoppedAt, e.Notes,
            e.RateEntries.OrderByDescending(r => r.RecordedAt).Select(r => new InfusionRateEntryDto(
                r.Id, r.RecordedAt, r.RecordedByUser!.FullName, r.Rate, r.RateUnit, r.Notes)).ToList())).ToList();
    }

    // ── Severity ─────────────────────────────────────────────────────────────
    public async Task<long> RecordSeverityAsync(long admissionId, RecordSeverityRequest request, CancellationToken ct = default)
    {
        await RequireActiveAdmissionAsync(admissionId, ct);
        var assessment = new IcuSeverityAssessment
        {
            AdmissionId = admissionId, AssessedAt = timeProvider.GetUtcNow(), AssessedByUserId = currentUser.UserId,
            SeverityLevel = request.SeverityLevel, Notes = request.Notes,
        };
        db.IcuSeverityAssessments.Add(assessment);
        await db.SaveChangesAsync(ct);
        return assessment.Id;
    }

    public async Task<IReadOnlyList<IcuSeverityAssessmentDto>> GetSeverityHistoryAsync(long admissionId, CancellationToken ct = default) =>
        await db.IcuSeverityAssessments.AsNoTracking().Where(a => a.AdmissionId == admissionId)
            .Include(a => a.AssessedByUser)
            .OrderByDescending(a => a.AssessedAt)
            .Select(a => new IcuSeverityAssessmentDto(a.Id, a.AdmissionId, a.AssessedAt, a.AssessedByUser!.FullName, a.SeverityLevel, a.Notes))
            .ToListAsync(ct);

    private async Task RequireActiveAdmissionAsync(long admissionId, CancellationToken ct)
    {
        var status = await db.Admissions.AsNoTracking().Where(a => a.Id == admissionId).Select(a => (AdmissionStatus?)a.Status).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Admission), admissionId);
        if (status != AdmissionStatus.Admitted)
        {
            throw new DomainException("ICU chart entries can only be recorded against an active admission.");
        }
    }
}
