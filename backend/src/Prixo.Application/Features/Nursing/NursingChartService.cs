using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Nursing;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Nursing;

/// <summary>
/// Nursing assessments, shift-tagged notes, risk scores (Pain/Fall/Bed Sore), care plans and
/// transfer notes (plan §10) — repeatable, admission-scoped charting in the same loose,
/// plain-save posture as IPD's ClinicalChartService. Vitals and Intake/Output stay on IPD's
/// existing IpVitals/IntakeOutputEntry tables (Phase 9 already built the "feeds charts" plumbing);
/// this service only adds the entries Phase 10 introduces.
/// </summary>
public class NursingChartService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> AddNursingAssessmentAsync(long admissionId, AddNursingAssessmentRequest request, CancellationToken ct = default)
    {
        await RequireActiveAdmissionAsync(admissionId, ct);
        var assessment = new NursingAssessment
        {
            AdmissionId = admissionId, TemplateCode = request.TemplateCode, AssessedAt = timeProvider.GetUtcNow(),
            AssessedByUserId = currentUser.UserId, DataJson = request.DataJson,
        };
        db.NursingAssessments.Add(assessment);
        await db.SaveChangesAsync(ct);
        return assessment.Id;
    }

    public async Task<IReadOnlyList<NursingAssessmentDto>> GetNursingAssessmentsAsync(long admissionId, CancellationToken ct = default) =>
        await db.NursingAssessments.AsNoTracking().Where(a => a.AdmissionId == admissionId)
            .OrderByDescending(a => a.AssessedAt)
            .Join(db.Users.AsNoTracking(), a => a.AssessedByUserId, u => u.Id, (a, u) =>
                new NursingAssessmentDto(a.Id, a.TemplateCode, a.AssessedAt, u.FullName, a.DataJson))
            .ToListAsync(ct);

    public async Task<long> AddNursingNoteAsync(long admissionId, AddNursingNoteRequest request, CancellationToken ct = default)
    {
        await RequireActiveAdmissionAsync(admissionId, ct);
        var note = new NursingNote
        {
            AdmissionId = admissionId, Shift = request.Shift, NoteAt = timeProvider.GetUtcNow(),
            RecordedByUserId = currentUser.UserId, Notes = request.Notes,
        };
        db.NursingNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return note.Id;
    }

    public async Task<IReadOnlyList<NursingNoteDto>> GetNursingNotesAsync(long admissionId, CancellationToken ct = default) =>
        await db.NursingNotes.AsNoTracking().Where(n => n.AdmissionId == admissionId)
            .OrderByDescending(n => n.NoteAt)
            .Join(db.Users.AsNoTracking(), n => n.RecordedByUserId, u => u.Id, (n, u) =>
                new NursingNoteDto(n.Id, n.Shift, n.NoteAt, u.FullName, n.Notes))
            .ToListAsync(ct);

    public async Task<long> AddRiskAssessmentAsync(long admissionId, AddRiskAssessmentRequest request, CancellationToken ct = default)
    {
        await RequireActiveAdmissionAsync(admissionId, ct);
        var total = NursingMath.SumScore(request.ItemScores.Values);
        var assessment = new RiskAssessment
        {
            AdmissionId = admissionId, AssessmentType = request.AssessmentType, AssessedAt = timeProvider.GetUtcNow(),
            AssessedByUserId = currentUser.UserId, ScoreDataJson = JsonSerializer.Serialize(request.ItemScores),
            TotalScore = total, RiskLevel = NursingMath.ResolveRiskLevel(request.AssessmentType, total),
        };
        db.RiskAssessments.Add(assessment);
        await db.SaveChangesAsync(ct);
        return assessment.Id;
    }

    public async Task<IReadOnlyList<RiskAssessmentDto>> GetRiskAssessmentsAsync(long admissionId, RiskAssessmentType? type, CancellationToken ct = default)
    {
        var query = db.RiskAssessments.AsNoTracking().Where(r => r.AdmissionId == admissionId);
        if (type is { } t) query = query.Where(r => r.AssessmentType == t);
        var rows = await query.OrderByDescending(r => r.AssessedAt)
            .Join(db.Users.AsNoTracking(), r => r.AssessedByUserId, u => u.Id, (r, u) =>
                new { r.Id, r.AssessmentType, r.AssessedAt, u.FullName, r.ScoreDataJson, r.TotalScore, r.RiskLevel })
            .ToListAsync(ct);
        return [.. rows.Select(r => new RiskAssessmentDto(
            r.Id, r.AssessmentType, r.AssessedAt, r.FullName,
            JsonSerializer.Deserialize<Dictionary<string, int>>(r.ScoreDataJson) ?? [], r.TotalScore, r.RiskLevel))];
    }

    public async Task<long> AddCarePlanAsync(long admissionId, AddCarePlanRequest request, CancellationToken ct = default)
    {
        await RequireActiveAdmissionAsync(admissionId, ct);
        var plan = new CarePlan
        {
            AdmissionId = admissionId, ProblemStatement = request.ProblemStatement, Goal = request.Goal,
            Interventions = request.Interventions, CreatedByUserId = currentUser.UserId,
        };
        db.CarePlans.Add(plan);
        await db.SaveChangesAsync(ct);
        return plan.Id;
    }

    public async Task<IReadOnlyList<CarePlanDto>> GetCarePlansAsync(long admissionId, CancellationToken ct = default) =>
        await db.CarePlans.AsNoTracking().Where(c => c.AdmissionId == admissionId)
            .OrderByDescending(c => c.CreatedAt)
            .Join(db.Users.AsNoTracking(), c => c.CreatedByUserId, u => u.Id, (c, u) =>
                new CarePlanDto(c.Id, c.ProblemStatement, c.Goal, c.Interventions, c.Status, u.FullName, c.CreatedAt, c.EvaluationNotes, c.ResolvedAt))
            .ToListAsync(ct);

    public async Task ResolveCarePlanAsync(long admissionId, long carePlanId, ResolveCarePlanRequest request, CancellationToken ct = default)
    {
        var plan = await db.CarePlans.FirstOrDefaultAsync(c => c.Id == carePlanId && c.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException(nameof(CarePlan), carePlanId);
        if (plan.Status == CarePlanStatus.Resolved)
        {
            throw new DomainException("Care plan is already resolved.");
        }
        plan.Status = CarePlanStatus.Resolved;
        plan.EvaluationNotes = request.EvaluationNotes;
        plan.ResolvedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task<long> AddTransferNoteAsync(long admissionId, AddTransferNoteRequest request, CancellationToken ct = default)
    {
        await RequireActiveAdmissionAsync(admissionId, ct);
        if (request.BedAllocationId is { } bedAllocationId
            && !await db.BedAllocations.AnyAsync(b => b.Id == bedAllocationId && b.AdmissionId == admissionId, ct))
        {
            throw new NotFoundException(nameof(BedAllocation), bedAllocationId);
        }
        var note = new TransferNote
        {
            AdmissionId = admissionId, BedAllocationId = request.BedAllocationId, TransferredAt = timeProvider.GetUtcNow(),
            HandoverNotes = request.HandoverNotes, TransferredByUserId = currentUser.UserId, ReceivedByUserId = request.ReceivedByUserId,
        };
        db.TransferNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return note.Id;
    }

    public async Task<IReadOnlyList<TransferNoteDto>> GetTransferNotesAsync(long admissionId, CancellationToken ct = default) =>
        await db.TransferNotes.AsNoTracking().Where(t => t.AdmissionId == admissionId)
            .Include(t => t.TransferredByUser).Include(t => t.ReceivedByUser)
            .OrderByDescending(t => t.TransferredAt)
            .Select(t => new TransferNoteDto(t.Id, t.TransferredAt, t.HandoverNotes, t.TransferredByUser!.FullName, t.ReceivedByUser != null ? t.ReceivedByUser.FullName : null))
            .ToListAsync(ct);

    private async Task RequireActiveAdmissionAsync(long admissionId, CancellationToken ct)
    {
        var status = await db.Admissions.AsNoTracking().Where(a => a.Id == admissionId).Select(a => (AdmissionStatus?)a.Status).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Admission), admissionId);
        if (status != AdmissionStatus.Admitted)
        {
            throw new DomainException("Nursing entries can only be recorded against an active admission.");
        }
    }
}
