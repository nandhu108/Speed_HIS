using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Er;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Er;

/// <summary>
/// The "fast clinical sheet" (find-or-create upsert, same posture as OT's OperativeNote)
/// plus observation-ward bed allocation — folded into the same service since allocating an
/// observation bed is really just what happens right after Disposition is set to
/// Observation, not an independent concern.
/// </summary>
public class EmergencyAssessmentService(IAppDbContext db, IActivityLogger activity, TimeProvider timeProvider)
{
    public async Task<AssessmentDto> SaveAssessmentAsync(long visitId, SaveAssessmentRequest request, CancellationToken ct = default)
    {
        var visit = await db.EmergencyVisits.FirstOrDefaultAsync(v => v.Id == visitId, ct) ?? throw new NotFoundException(nameof(EmergencyVisit), visitId);
        if (!await db.Doctors.AnyAsync(d => d.Id == request.AssessedByDoctorId, ct))
        {
            throw new NotFoundException(nameof(Doctor), request.AssessedByDoctorId);
        }

        var assessment = await db.EmergencyAssessments.FirstOrDefaultAsync(a => a.EmergencyVisitId == visitId, ct);
        if (assessment is null)
        {
            assessment = new EmergencyAssessment { EmergencyVisitId = visitId, AssessedByDoctorId = request.AssessedByDoctorId, AssessedAt = timeProvider.GetUtcNow() };
            db.EmergencyAssessments.Add(assessment);
        }
        assessment.AssessedByDoctorId = request.AssessedByDoctorId;
        assessment.History = request.History;
        assessment.ExaminationFindings = request.ExaminationFindings;
        assessment.ProvisionalDiagnosis = request.ProvisionalDiagnosis;
        assessment.TreatmentGiven = request.TreatmentGiven;
        assessment.Disposition = request.Disposition;
        assessment.DispositionNotes = request.DispositionNotes;
        assessment.TransferredToFacility = request.TransferredToFacility;
        assessment.TransferSummary = request.TransferSummary;

        visit.Status = request.Disposition is null ? EmergencyStatus.UnderTreatment : EmergencyStatus.Disposed;
        if (request.Disposition is { } disposition)
        {
            await activity.LogAsync(ActivityEvents.EmergencyDisposition, $"ER visit {visit.VisitNumber} disposed: {disposition}", ct: ct);
        }
        await db.SaveChangesAsync(ct);

        var doctorName = await db.Doctors.Where(d => d.Id == assessment.AssessedByDoctorId).Select(d => d.User!.FullName).FirstAsync(ct);
        return ToDto(assessment, doctorName);
    }

    public async Task<AssessmentDto> GetAssessmentAsync(long visitId, CancellationToken ct = default)
    {
        var a = await db.EmergencyAssessments.AsNoTracking().Include(x => x.AssessedByDoctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(x => x.EmergencyVisitId == visitId, ct) ?? throw new NotFoundException(nameof(EmergencyAssessment), visitId);
        return ToDto(a, a.AssessedByDoctor!.User!.FullName);
    }

    public async Task<long> AllocateObservationAsync(long visitId, AllocateObservationRequest request, CancellationToken ct = default)
    {
        if (!await db.EmergencyVisits.AnyAsync(v => v.Id == visitId, ct))
        {
            throw new NotFoundException(nameof(EmergencyVisit), visitId);
        }
        if (!await db.Beds.AnyAsync(b => b.Id == request.BedId, ct))
        {
            throw new NotFoundException(nameof(Bed), request.BedId);
        }
        if (await db.ObservationAllocations.AnyAsync(o => o.BedId == request.BedId && o.ReleasedAt == null, ct))
        {
            throw new DomainException("This bed is already occupied by another observation allocation.");
        }

        var allocation = new ObservationAllocation { EmergencyVisitId = visitId, BedId = request.BedId, AllocatedAt = timeProvider.GetUtcNow() };
        db.ObservationAllocations.Add(allocation);
        await db.SaveChangesAsync(ct);
        return allocation.Id;
    }

    public async Task ReleaseObservationAsync(long allocationId, CancellationToken ct = default)
    {
        var allocation = await db.ObservationAllocations.FirstOrDefaultAsync(o => o.Id == allocationId, ct)
            ?? throw new NotFoundException(nameof(ObservationAllocation), allocationId);
        if (allocation.ReleasedAt is not null)
        {
            throw new DomainException("This observation allocation is already released.");
        }
        allocation.ReleasedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ObservationAllocationDto>> GetObservationsAsync(long visitId, CancellationToken ct = default) =>
        await db.ObservationAllocations.AsNoTracking().Where(o => o.EmergencyVisitId == visitId)
            .Include(o => o.Bed).ThenInclude(b => b!.Room).ThenInclude(r => r!.Ward)
            .OrderByDescending(o => o.AllocatedAt)
            .Select(o => new ObservationAllocationDto(
                o.Id, o.EmergencyVisitId, o.BedId, o.Bed!.Room!.Ward!.Name, o.Bed.Room.Name, o.Bed.Code, o.AllocatedAt, o.ReleasedAt))
            .ToListAsync(ct);

    private static AssessmentDto ToDto(EmergencyAssessment a, string doctorName) => new(
        a.Id, a.EmergencyVisitId, a.AssessedByDoctorId, doctorName, a.AssessedAt,
        a.History, a.ExaminationFindings, a.ProvisionalDiagnosis, a.TreatmentGiven,
        a.Disposition, a.DispositionNotes, a.TransferredToFacility, a.TransferSummary);
}
