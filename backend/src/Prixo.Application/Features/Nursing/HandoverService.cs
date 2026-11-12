using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Nursing;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Nursing;

/// <summary>
/// Ward-level, per-shift SBAR handover (plan §10). One handover per ward/date/shift — the DB's
/// partial unique index is the concurrency guard; CreateAsync's friendly pre-check just gives a
/// nicer error before hitting it. Nothing here touches beds/admissions — it's a communication
/// record, not a workflow step.
/// </summary>
public class HandoverService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<WardPatientDto>> GetWardPatientsAsync(long wardId, CancellationToken ct = default) =>
        await db.Admissions.AsNoTracking()
            .Where(a => a.Status == AdmissionStatus.Admitted && a.CurrentBed!.Room!.WardId == wardId)
            .Include(a => a.Patient).Include(a => a.CurrentBed)
            .OrderBy(a => a.CurrentBed!.Code)
            .Select(a => new WardPatientDto(a.Id, a.AdmissionNumber, a.Patient!.FirstName + " " + a.Patient.LastName, a.CurrentBed!.Code))
            .ToListAsync(ct);

    public async Task<long> CreateAsync(CreateShiftHandoverRequest request, CancellationToken ct = default)
    {
        if (!await db.Wards.AnyAsync(w => w.Id == request.WardId, ct))
        {
            throw new NotFoundException(nameof(Ward), request.WardId);
        }
        if (await db.ShiftHandovers.AnyAsync(h => h.WardId == request.WardId && h.ShiftDate == request.ShiftDate && h.Shift == request.Shift, ct))
        {
            throw new DomainException("A handover has already been recorded for this ward/date/shift.");
        }
        foreach (var admissionId in request.PatientNotes.Select(p => p.AdmissionId).Distinct())
        {
            if (!await db.Admissions.AnyAsync(a => a.Id == admissionId, ct))
            {
                throw new NotFoundException("Admission", admissionId);
            }
        }

        var handover = new ShiftHandover
        {
            BranchId = RequireBranch(), WardId = request.WardId, ShiftDate = request.ShiftDate, Shift = request.Shift,
            HandoverFromUserId = currentUser.UserId, HandoverToUserId = request.HandoverToUserId,
            GeneralNotes = request.GeneralNotes, HandedOverAt = timeProvider.GetUtcNow(),
            PatientNotes = [.. request.PatientNotes.Select(p => new PatientHandoverNote
            {
                AdmissionId = p.AdmissionId, Situation = p.Situation, Background = p.Background,
                Assessment = p.Assessment, Recommendation = p.Recommendation,
            })],
        };
        db.ShiftHandovers.Add(handover);
        await db.SaveChangesAsync(ct);
        return handover.Id;
    }

    public async Task<IReadOnlyList<ShiftHandoverListItemDto>> GetByWardAsync(long? wardId, DateOnly? date, CancellationToken ct = default)
    {
        var query = db.ShiftHandovers.AsNoTracking().AsQueryable();
        if (wardId is { } w) query = query.Where(h => h.WardId == w);
        if (date is { } d) query = query.Where(h => h.ShiftDate == d);

        return await query.Include(h => h.Ward).Include(h => h.HandoverFromUser).Include(h => h.HandoverToUser).Include(h => h.PatientNotes)
            .OrderByDescending(h => h.ShiftDate).ThenByDescending(h => h.Shift)
            .Select(h => new ShiftHandoverListItemDto(
                h.Id, h.WardId, h.Ward!.Name, h.ShiftDate, h.Shift, h.HandoverFromUser!.FullName,
                h.HandoverToUser != null ? h.HandoverToUser.FullName : null, h.HandedOverAt, h.PatientNotes.Count))
            .ToListAsync(ct);
    }

    public async Task<ShiftHandoverDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var h = await db.ShiftHandovers.AsNoTracking().Include(x => x.Ward).Include(x => x.HandoverFromUser).Include(x => x.HandoverToUser)
            .Include(x => x.PatientNotes).ThenInclude(n => n.Admission).ThenInclude(a => a!.Patient)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException(nameof(ShiftHandover), id);

        return new ShiftHandoverDetailDto(
            h.Id, h.WardId, h.Ward!.Name, h.ShiftDate, h.Shift, h.HandoverFromUser!.FullName,
            h.HandoverToUser != null ? h.HandoverToUser.FullName : null, h.GeneralNotes, h.HandedOverAt,
            [.. h.PatientNotes.Select(n => new PatientHandoverNoteDto(
                n.Id, n.AdmissionId, n.Admission!.AdmissionNumber, n.Admission.Patient!.FirstName + " " + n.Admission.Patient.LastName,
                n.Situation, n.Background, n.Assessment, n.Recommendation))]);
    }

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
