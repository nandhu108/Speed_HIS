using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Emr;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Emr;

/// <summary>
/// The diagnosis registry (plan §11) — a manually-curated, patient-level Active/Resolved
/// problem list. Distinct from and not synced with OPD's per-visit VisitDiagnosis.
/// </summary>
public class ProblemListService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> AddAsync(long patientId, AddProblemRequest request, CancellationToken ct = default)
    {
        if (!await db.Patients.AnyAsync(p => p.Id == patientId, ct))
        {
            throw new NotFoundException(nameof(Patient), patientId);
        }
        if (request.IcdCodeId is { } icdCodeId && !await db.IcdCodes.AnyAsync(i => i.Id == icdCodeId, ct))
        {
            throw new NotFoundException(nameof(IcdCode), icdCodeId);
        }

        var entry = new ProblemListEntry
        {
            PatientId = patientId, IcdCodeId = request.IcdCodeId, DiagnosisText = request.DiagnosisText,
            IdentifiedDate = request.IdentifiedDate, Notes = request.Notes, RecordedByUserId = currentUser.UserId,
        };
        db.ProblemListEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id;
    }

    public async Task<IReadOnlyList<ProblemListEntryDto>> GetAsync(long patientId, ProblemStatus? status, CancellationToken ct = default)
    {
        var query = db.ProblemListEntries.AsNoTracking().Where(p => p.PatientId == patientId);
        if (status is { } s) query = query.Where(p => p.Status == s);
        return await query.Include(p => p.IcdCode)
            .OrderByDescending(p => p.IdentifiedDate)
            .Join(db.Users.AsNoTracking(), p => p.RecordedByUserId, u => u.Id, (p, u) =>
                new ProblemListEntryDto(p.Id, p.IcdCodeId, p.IcdCode != null ? p.IcdCode.Code : null, p.DiagnosisText,
                    p.Status, p.IdentifiedDate, p.ResolvedDate, p.Notes, u.FullName))
            .ToListAsync(ct);
    }

    public async Task ResolveAsync(long patientId, long problemId, CancellationToken ct = default)
    {
        var entry = await db.ProblemListEntries.FirstOrDefaultAsync(p => p.Id == problemId && p.PatientId == patientId, ct)
            ?? throw new NotFoundException(nameof(ProblemListEntry), problemId);
        if (entry.Status == ProblemStatus.Resolved)
        {
            throw new DomainException("Problem is already resolved.");
        }
        entry.Status = ProblemStatus.Resolved;
        entry.ResolvedDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);
    }
}
