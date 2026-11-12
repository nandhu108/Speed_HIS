using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Emr;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Emr;

/// <summary>Ad-hoc clinical notes (plan §11 "specialty templates" — NoteType is free text, the frontend owns any per-type prompts).</summary>
public class ClinicalNoteService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> AddAsync(long patientId, AddClinicalNoteRequest request, CancellationToken ct = default)
    {
        if (!await db.Patients.AnyAsync(p => p.Id == patientId, ct))
        {
            throw new NotFoundException(nameof(Patient), patientId);
        }
        if (request.OpVisitId is { } opVisitId && !await db.OpVisits.AnyAsync(v => v.Id == opVisitId && v.PatientId == patientId, ct))
        {
            throw new NotFoundException(nameof(OpVisit), opVisitId);
        }
        if (request.AdmissionId is { } admissionId && !await db.Admissions.AnyAsync(a => a.Id == admissionId && a.PatientId == patientId, ct))
        {
            throw new NotFoundException(nameof(Admission), admissionId);
        }

        var note = new ClinicalNote
        {
            PatientId = patientId, NoteType = request.NoteType, Title = request.Title, Content = request.Content,
            NoteAt = timeProvider.GetUtcNow(), RecordedByUserId = currentUser.UserId,
            OpVisitId = request.OpVisitId, AdmissionId = request.AdmissionId,
        };
        db.ClinicalNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return note.Id;
    }

    public async Task<IReadOnlyList<ClinicalNoteDto>> GetAsync(long patientId, CancellationToken ct = default) =>
        await db.ClinicalNotes.AsNoTracking().Where(n => n.PatientId == patientId)
            .OrderByDescending(n => n.NoteAt)
            .Join(db.Users.AsNoTracking(), n => n.RecordedByUserId, u => u.Id, (n, u) =>
                new ClinicalNoteDto(n.Id, n.NoteType, n.Title, n.Content, n.NoteAt, u.FullName, n.OpVisitId, n.AdmissionId))
            .ToListAsync(ct);
}
