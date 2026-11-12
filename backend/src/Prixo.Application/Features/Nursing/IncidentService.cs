using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Nursing;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Nursing;

/// <summary>
/// Incident reporting (plan §10): Report → UnderReview → Closed. Deliberately shallow — no
/// RCA/CAPA fields, no severity-driven routing — Phase 20 (Quality/NABH) merges this into the
/// org-wide register with that machinery later.
/// </summary>
public class IncidentService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> ReportAsync(ReportIncidentRequest request, CancellationToken ct = default)
    {
        if (request.PatientId is { } patientId && !await db.Patients.AnyAsync(p => p.Id == patientId, ct))
        {
            throw new NotFoundException(nameof(Patient), patientId);
        }
        if (request.AdmissionId is { } admissionId && !await db.Admissions.AnyAsync(a => a.Id == admissionId, ct))
        {
            throw new NotFoundException(nameof(Admission), admissionId);
        }

        var incident = new Incident
        {
            BranchId = RequireBranch(), IncidentDate = request.IncidentDate, IncidentType = request.IncidentType,
            Location = request.Location, Severity = request.Severity, Description = request.Description,
            ImmediateActionTaken = request.ImmediateActionTaken, PatientId = request.PatientId, AdmissionId = request.AdmissionId,
            ReportedByUserId = currentUser.UserId, Status = IncidentStatus.Reported, SourceModule = "NURSING",
        };
        db.Incidents.Add(incident);
        await db.SaveChangesAsync(ct);
        return incident.Id;
    }

    public async Task<PagedResult<IncidentDto>> GetPagedAsync(IncidentQuery query, CancellationToken ct = default)
    {
        var incidents = db.Incidents.AsNoTracking().AsQueryable();
        if (query.Status is { } status) incidents = incidents.Where(i => i.Status == status);
        if (query.Severity is { } severity) incidents = incidents.Where(i => i.Severity == severity);
        if (query.FromDate is { } from) incidents = incidents.Where(i => i.IncidentDate >= new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (query.ToDate is { } to) incidents = incidents.Where(i => i.IncidentDate < new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (query.SentinelOnly == true) incidents = incidents.Where(i => i.IsSentinelEvent);

        var totalCount = await incidents.CountAsync(ct);
        var items = await incidents
            .Include(i => i.Patient).Include(i => i.Admission).Include(i => i.ReportedByUser).Include(i => i.ReviewedByUser)
            .OrderByDescending(i => i.IncidentDate)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(i => ToDto(i))
            .ToListAsync(ct);
        return PagedResult<IncidentDto>.Create(items, query.Page, query.PageSize, totalCount);
    }

    public async Task ReviewAsync(long id, ReviewIncidentRequest request, CancellationToken ct = default)
    {
        var incident = await db.Incidents.FirstOrDefaultAsync(i => i.Id == id, ct) ?? throw new NotFoundException(nameof(Incident), id);
        if (incident.Status == IncidentStatus.Closed)
        {
            throw new DomainException("A closed incident cannot be reviewed.");
        }
        incident.Status = IncidentStatus.UnderReview;
        incident.ReviewNotes = request.ReviewNotes;
        incident.ReviewedByUserId = currentUser.UserId;
        await db.SaveChangesAsync(ct);
    }

    public async Task CloseAsync(long id, CloseIncidentRequest request, CancellationToken ct = default)
    {
        var incident = await db.Incidents.FirstOrDefaultAsync(i => i.Id == id, ct) ?? throw new NotFoundException(nameof(Incident), id);
        if (incident.Status == IncidentStatus.Closed)
        {
            throw new DomainException("Incident is already closed.");
        }
        incident.Status = IncidentStatus.Closed;
        incident.ReviewNotes = request.ReviewNotes ?? incident.ReviewNotes;
        incident.ReviewedByUserId ??= currentUser.UserId;
        incident.ClosedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Quality's RCA action (QUALITY.INCIDENT.RCA) — records root-cause analysis and the sentinel-event flag without touching the Report/Review/Close state machine.</summary>
    public async Task RecordRcaAsync(long id, RecordIncidentRcaRequest request, CancellationToken ct = default)
    {
        var incident = await db.Incidents.FirstOrDefaultAsync(i => i.Id == id, ct) ?? throw new NotFoundException(nameof(Incident), id);
        incident.RootCauseAnalysis = request.RootCauseAnalysis;
        incident.IsSentinelEvent = request.IsSentinelEvent;
        await db.SaveChangesAsync(ct);
    }

    private static IncidentDto ToDto(Incident i) => new(
        i.Id, i.IncidentDate, i.IncidentType, i.Location, i.Severity, i.Description, i.ImmediateActionTaken,
        i.PatientId, i.Patient != null ? i.Patient.FirstName + " " + i.Patient.LastName : null,
        i.AdmissionId, i.Admission != null ? i.Admission.AdmissionNumber : null,
        i.ReportedByUser!.FullName, i.Status, i.ReviewNotes, i.ReviewedByUser != null ? i.ReviewedByUser.FullName : null, i.ClosedAt,
        i.SourceModule, i.IsSentinelEvent, i.RootCauseAnalysis);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
