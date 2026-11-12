using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Mrd;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Mrd;

/// <summary>
/// Record Retention's Destruction Register (plan §17). Eligibility (<see cref="MrdMath.EligibleDate"/>)
/// is computed on read, not stored, until MRD flags a file — creating the register row is the
/// deliberate act that starts the Eligible → Approved → Destroyed lifecycle.
/// </summary>
public class DestructionRegisterService(IAppDbContext db, ICurrentUser currentUser, IActivityLogger activity, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<EligibleFileDto>> GetEligibleAsync(long retentionPolicyId, CancellationToken ct = default)
    {
        var policy = await db.RetentionPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == retentionPolicyId, ct)
            ?? throw new NotFoundException(nameof(RetentionPolicy), retentionPolicyId);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var candidates = await db.RecordFiles.AsNoTracking()
            .Where(f => f.Status == RecordFileStatus.Active && f.Admission!.DischargedAt != null)
            .Where(f => !db.DestructionRegisterEntries.Any(d => d.RecordFileId == f.Id))
            .Include(f => f.Admission).ThenInclude(a => a!.Patient)
            .ToListAsync(ct);

        return candidates
            .Select(f => (File: f, Eligible: MrdMath.EligibleDate(DateOnly.FromDateTime(f.Admission!.DischargedAt!.Value.UtcDateTime), policy.RetentionYears)))
            .Where(x => x.Eligible <= today)
            .Select(x => new EligibleFileDto(
                x.File.Id, x.File.FileNumber, x.File.Admission!.PatientId,
                x.File.Admission.Patient!.FirstName + " " + x.File.Admission.Patient.LastName, x.File.Admission.DischargedAt,
                policy.Id, policy.Name, x.Eligible))
            .ToList();
    }

    public async Task<long> CreateAsync(CreateDestructionEntryRequest request, CancellationToken ct = default)
    {
        var file = await db.RecordFiles.Include(f => f.Admission).FirstOrDefaultAsync(f => f.Id == request.RecordFileId, ct)
            ?? throw new NotFoundException(nameof(RecordFile), request.RecordFileId);
        var policy = await db.RetentionPolicies.FirstOrDefaultAsync(p => p.Id == request.RetentionPolicyId, ct)
            ?? throw new NotFoundException(nameof(RetentionPolicy), request.RetentionPolicyId);
        if (file.Admission?.DischargedAt is null)
        {
            throw new DomainException("Only a file for a discharged admission can be flagged for destruction.");
        }
        if (await db.DestructionRegisterEntries.AnyAsync(d => d.RecordFileId == request.RecordFileId, ct))
        {
            throw new DomainException("This file already has a destruction register entry.");
        }

        var entry = new DestructionRegisterEntry
        {
            RecordFileId = request.RecordFileId,
            RetentionPolicyId = request.RetentionPolicyId,
            EligibleDate = MrdMath.EligibleDate(DateOnly.FromDateTime(file.Admission.DischargedAt.Value.UtcDateTime), policy.RetentionYears),
        };
        db.DestructionRegisterEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.Id;
    }

    public async Task ApproveAsync(long id, CancellationToken ct = default)
    {
        var entry = await RequireAsync(id, ct);
        if (entry.Status != DestructionStatus.Eligible)
        {
            throw new DomainException($"This entry is {entry.Status}; expected Eligible.");
        }
        entry.Status = DestructionStatus.Approved;
        entry.ApprovedByUserId = currentUser.UserId;
        entry.ApprovedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task DestroyAsync(long id, DestroyRequest request, CancellationToken ct = default)
    {
        var entry = await db.DestructionRegisterEntries.Include(e => e.RecordFile).FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException(nameof(DestructionRegisterEntry), id);
        if (entry.Status != DestructionStatus.Approved)
        {
            throw new DomainException($"This entry is {entry.Status}; expected Approved.");
        }
        var now = timeProvider.GetUtcNow();
        entry.Status = DestructionStatus.Destroyed;
        entry.DestroyedByUserId = currentUser.UserId;
        entry.DestroyedAt = now;
        entry.DestructionMethod = request.DestructionMethod;
        entry.Notes = request.Notes;

        entry.RecordFile!.Status = RecordFileStatus.Destroyed;
        entry.RecordFile.DestroyedAt = now;

        await activity.LogAsync(ActivityEvents.MrdRecordDestroyed, $"Medical record file {entry.RecordFile.FileNumber} destroyed", ct: ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DestructionRegisterEntryDto>> GetAllAsync(DestructionRegisterQuery query, CancellationToken ct = default)
    {
        var entries = db.DestructionRegisterEntries.AsNoTracking()
            .Include(e => e.RecordFile).Include(e => e.RetentionPolicy)
            .Include(e => e.ApprovedByUser).Include(e => e.DestroyedByUser)
            .AsQueryable();
        if (query.Status is { } status) entries = entries.Where(e => e.Status == status);

        return await entries.OrderByDescending(e => e.EligibleDate)
            .Select(e => new DestructionRegisterEntryDto(
                e.Id, e.RecordFileId, e.RecordFile!.FileNumber, e.RetentionPolicyId, e.RetentionPolicy!.Name,
                e.EligibleDate, e.Status, e.ApprovedByUser != null ? e.ApprovedByUser.FullName : null, e.ApprovedAt,
                e.DestroyedByUser != null ? e.DestroyedByUser.FullName : null, e.DestroyedAt, e.DestructionMethod, e.Notes))
            .ToListAsync(ct);
    }

    private async Task<DestructionRegisterEntry> RequireAsync(long id, CancellationToken ct) =>
        await db.DestructionRegisterEntries.FirstOrDefaultAsync(e => e.Id == id, ct) ?? throw new NotFoundException(nameof(DestructionRegisterEntry), id);
}
