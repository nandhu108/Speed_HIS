using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Mrd;
using Prixo.Domain.Enums;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Mrd;

/// <summary>
/// ICD Coding worklist (plan §17: "coder worklist for discharged cases, coding completeness").
/// Every discharged admission is a candidate row; a <see cref="CodingWorklistEntry"/> is
/// created lazily the first time a coder marks it coded (find-or-create upsert, same idiom
/// as EmergencyAssessment). Diagnosis codes themselves are read from EMR/IPD, not owned here.
/// </summary>
public class CodingWorklistService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<CodingWorklistItemDto>> GetWorklistAsync(CodingWorklistQuery query, CancellationToken ct = default)
    {
        var rows = await db.Admissions.AsNoTracking()
            .Where(a => a.Status == AdmissionStatus.Discharged)
            .Include(a => a.Patient)
            .Select(a => new
            {
                Admission = a,
                Entry = db.CodingWorklistEntries.AsNoTracking().FirstOrDefault(c => c.AdmissionId == a.Id),
            })
            .OrderByDescending(x => x.Admission.DischargedAt)
            .ToListAsync(ct);

        var coderIds = rows.Where(x => x.Entry?.CodedByUserId is not null).Select(x => x.Entry!.CodedByUserId!.Value).Distinct().ToList();
        var coderNames = await db.Users.AsNoTracking().Where(u => coderIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var dtos = rows.Select(x => new CodingWorklistItemDto(
            x.Admission.Id, x.Admission.AdmissionNumber, x.Admission.PatientId,
            x.Admission.Patient!.FirstName + " " + x.Admission.Patient.LastName, x.Admission.Patient.Uhid,
            x.Admission.DischargedAt, x.Entry?.Status ?? CodingStatus.Pending,
            x.Entry?.CodedByUserId is { } coderId ? coderNames.GetValueOrDefault(coderId) : null, x.Entry?.CodedAt, x.Entry?.Notes));

        if (query.OnlyPending) dtos = dtos.Where(d => d.Status == CodingStatus.Pending);
        return dtos.ToList();
    }

    public async Task MarkCodedAsync(long admissionId, MarkCodedRequest request, CancellationToken ct = default)
    {
        var admission = await db.Admissions.FirstOrDefaultAsync(a => a.Id == admissionId, ct)
            ?? throw new NotFoundException(nameof(Admission), admissionId);
        if (admission.Status != AdmissionStatus.Discharged)
        {
            throw new DomainException("Only a discharged admission can be coded.");
        }

        var entry = await db.CodingWorklistEntries.FirstOrDefaultAsync(c => c.AdmissionId == admissionId, ct);
        if (entry is null)
        {
            entry = new CodingWorklistEntry { AdmissionId = admissionId };
            db.CodingWorklistEntries.Add(entry);
        }
        entry.Status = CodingStatus.Coded;
        entry.CodedByUserId = currentUser.UserId;
        entry.CodedAt = timeProvider.GetUtcNow();
        entry.Notes = request.Notes;
        await db.SaveChangesAsync(ct);
    }
}
