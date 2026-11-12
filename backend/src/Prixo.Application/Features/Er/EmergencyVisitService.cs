using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Er;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Er;

/// <summary>
/// The ER visit lifecycle — register (rapid; PatientId always points at a real Patient row,
/// created via Phase 3's existing minimal-fields RegisterAsync for "unknown" patients),
/// triage (ESI level + basic vitals, once per visit), the auto-priority queue, and cancel.
/// Mirrors OpVisitService's token-generation idiom exactly (EncodeDate resets the token
/// sequence daily) but scoped per branch, not per doctor — the whole ER shares one token
/// sequence per day.
/// </summary>
public class EmergencyVisitService(
    IAppDbContext db, ISequenceService sequences, ICurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider)
{
    public async Task<long> RegisterAsync(RegisterEmergencyVisitRequest request, CancellationToken ct = default)
    {
        var branchId = RequireBranch();
        if (!await db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
        {
            throw new NotFoundException(nameof(Patient), request.PatientId);
        }

        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var number = await sequences.NextValueAsync(SequenceKeys.EmergencyVisitNumber, branchId, today.Year, ct);
        var token = await sequences.NextValueAsync(SequenceKeys.EmergencyToken, branchId, EncodeDate(today), ct);

        var visit = new EmergencyVisit
        {
            BranchId = branchId,
            PatientId = request.PatientId,
            VisitNumber = $"ER-{today.Year}-{number.ToString().PadLeft(6, '0')}",
            TokenNumber = (int)token,
            ArrivedAt = now,
            ArrivalMode = request.ArrivalMode,
            IsMlc = request.IsMlc,
            RegisteredByUserId = currentUser.UserId,
        };
        db.EmergencyVisits.Add(visit);
        await activity.LogAsync(ActivityEvents.EmergencyVisitRegistered, $"ER visit {visit.VisitNumber} registered", ct: ct);
        await db.SaveChangesAsync(ct);
        return visit.Id;
    }

    public async Task<TriageDto> RecordTriageAsync(long id, RecordTriageRequest request, CancellationToken ct = default)
    {
        var visit = await db.EmergencyVisits.FirstOrDefaultAsync(v => v.Id == id, ct) ?? throw new NotFoundException(nameof(EmergencyVisit), id);
        if (visit.Status is EmergencyStatus.Disposed or EmergencyStatus.Cancelled)
        {
            throw new DomainException($"A visit that is {visit.Status} cannot be triaged.");
        }
        if (request.AssignDoctorId is { } doctorId && !await db.Doctors.AnyAsync(d => d.Id == doctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), doctorId);
        }

        var triage = await db.Triages.FirstOrDefaultAsync(t => t.EmergencyVisitId == id, ct);
        var now = timeProvider.GetUtcNow();
        if (triage is null)
        {
            triage = new Triage { EmergencyVisitId = id, ChiefComplaint = request.ChiefComplaint };
            db.Triages.Add(triage);
        }
        triage.TriagedAt = now;
        triage.TriagedByUserId = currentUser.UserId;
        triage.EsiLevel = request.EsiLevel;
        triage.ChiefComplaint = request.ChiefComplaint;
        triage.TemperatureF = request.TemperatureF;
        triage.PulseRate = request.PulseRate;
        triage.RespiratoryRate = request.RespiratoryRate;
        triage.BloodPressureSystolic = request.BloodPressureSystolic;
        triage.BloodPressureDiastolic = request.BloodPressureDiastolic;
        triage.SpO2 = request.SpO2;
        triage.Notes = request.Notes;

        visit.Status = EmergencyStatus.Triaged;
        if (request.AssignDoctorId is { } assignedId) visit.AssignedDoctorId = assignedId;
        await db.SaveChangesAsync(ct);

        var triagedByName = await db.Users.Where(u => u.Id == triage.TriagedByUserId).Select(u => u.FullName).FirstAsync(ct);
        return ToTriageDto(triage, triagedByName);
    }

    public async Task CancelAsync(long id, CancelEmergencyVisitRequest request, CancellationToken ct = default)
    {
        var visit = await db.EmergencyVisits.FirstOrDefaultAsync(v => v.Id == id, ct) ?? throw new NotFoundException(nameof(EmergencyVisit), id);
        if (visit.Status is EmergencyStatus.Disposed or EmergencyStatus.Cancelled)
        {
            throw new DomainException($"A visit that is {visit.Status} cannot be cancelled.");
        }
        visit.Status = EmergencyStatus.Cancelled;
        visit.CancelReason = request.Reason;
        visit.CancelledAt = timeProvider.GetUtcNow();
        await activity.LogAsync(ActivityEvents.EmergencyVisitCancelled, $"ER visit {visit.VisitNumber} cancelled: {request.Reason}", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The auto-priority ER queue — untriaged patients surface first (they need immediate triage), then by ESI level, then by arrival time.</summary>
    public async Task<IReadOnlyList<EmergencyVisitListItemDto>> GetQueueAsync(EmergencyVisitQuery query, CancellationToken ct = default)
    {
        var visits = db.EmergencyVisits.AsNoTracking();
        if (query.Status is { } status) visits = visits.Where(v => v.Status == status);
        else visits = visits.Where(v => v.Status != EmergencyStatus.Disposed && v.Status != EmergencyStatus.Cancelled);
        if (query.FromDate is { } from)
        {
            var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            visits = visits.Where(v => v.ArrivedAt >= start);
        }
        if (query.ToDate is { } to)
        {
            var end = new DateTimeOffset(to.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1);
            visits = visits.Where(v => v.ArrivedAt < end);
        }

        var rows = await visits.Include(v => v.Patient).Include(v => v.AssignedDoctor).ThenInclude(d => d!.User)
            .Select(v => new
            {
                Visit = v,
                Triage = db.Triages.AsNoTracking().FirstOrDefault(t => t.EmergencyVisitId == v.Id),
                Disposition = db.EmergencyAssessments.AsNoTracking()
                    .Where(a => a.EmergencyVisitId == v.Id).Select(a => (DispositionType?)a.Disposition).FirstOrDefault(),
                AlreadyAdmitted = db.Admissions.Any(a => a.ReferringEmergencyVisitId == v.Id),
            })
            .ToListAsync(ct);

        return rows
            .OrderBy(r => ErMath.QueuePriority(r.Triage?.EsiLevel))
            .ThenBy(r => r.Visit.ArrivedAt)
            .Select(r => new EmergencyVisitListItemDto(
                r.Visit.Id, r.Visit.VisitNumber, r.Visit.TokenNumber, r.Visit.PatientId,
                r.Visit.Patient!.FirstName + " " + r.Visit.Patient.LastName, r.Visit.Patient.Uhid, r.Visit.ArrivedAt, r.Visit.ArrivalMode,
                r.Visit.Status, r.Triage?.EsiLevel, r.Visit.AssignedDoctor != null ? r.Visit.AssignedDoctor.User!.FullName : null, r.Visit.IsMlc,
                r.Disposition, r.AlreadyAdmitted))
            .ToList();
    }

    public async Task<EmergencyVisitDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var v = await db.EmergencyVisits.AsNoTracking()
            .Include(x => x.Patient).Include(x => x.AssignedDoctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(EmergencyVisit), id);

        var triage = await db.Triages.AsNoTracking().Include(t => t.TriagedByUser).FirstOrDefaultAsync(t => t.EmergencyVisitId == id, ct);

        return new EmergencyVisitDetailDto(
            v.Id, v.VisitNumber, v.TokenNumber, v.PatientId, v.Patient!.FirstName + " " + v.Patient.LastName, v.Patient.Uhid,
            v.ArrivedAt, v.ArrivalMode, v.IsMlc, v.Status, v.AssignedDoctorId,
            v.AssignedDoctor != null ? v.AssignedDoctor.User!.FullName : null, v.CancelReason,
            triage is null ? null : ToTriageDto(triage, triage.TriagedByUser!.FullName));
    }

    private static TriageDto ToTriageDto(Triage t, string triagedByName) => new(
        t.Id, t.TriagedAt, triagedByName, t.EsiLevel, t.ChiefComplaint,
        t.TemperatureF, t.PulseRate, t.RespiratoryRate, t.BloodPressureSystolic, t.BloodPressureDiastolic, t.SpO2, t.Notes);

    private static int EncodeDate(DateOnly date) => (date.Year * 10000) + (date.Month * 100) + date.Day;
    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
