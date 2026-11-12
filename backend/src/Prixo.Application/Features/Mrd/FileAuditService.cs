using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Mrd;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Mrd;

/// <summary>
/// Discharge File Audit + Missing Document Tracking (plan §17). One row per admission
/// (find-or-create upsert, same idiom as EmergencyAssessment); <see cref="MrdMath.IsAuditComplete"/>
/// decides <see cref="FileAudit.IsComplete"/> from the checklist flags every save.
/// </summary>
public class FileAuditService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<long> SaveAsync(long admissionId, SaveFileAuditRequest request, CancellationToken ct = default)
    {
        if (!await db.Admissions.AnyAsync(a => a.Id == admissionId, ct))
        {
            throw new NotFoundException(nameof(Admission), admissionId);
        }

        var audit = await db.FileAudits.FirstOrDefaultAsync(a => a.AdmissionId == admissionId, ct);
        if (audit is null)
        {
            audit = new FileAudit { AdmissionId = admissionId };
            db.FileAudits.Add(audit);
        }
        audit.DischargeSummarySigned = request.DischargeSummarySigned;
        audit.ConsentsPresent = request.ConsentsPresent;
        audit.ProgressNotesComplete = request.ProgressNotesComplete;
        audit.InvestigationReportsPresent = request.InvestigationReportsPresent;
        audit.NursingRecordsComplete = request.NursingRecordsComplete;
        audit.MissingDocumentsNotes = request.MissingDocumentsNotes;
        audit.IsComplete = MrdMath.IsAuditComplete(
            request.DischargeSummarySigned, request.ConsentsPresent, request.ProgressNotesComplete,
            request.InvestigationReportsPresent, request.NursingRecordsComplete);
        audit.AuditedByUserId = currentUser.UserId;
        audit.AuditedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return audit.Id;
    }

    public async Task<FileAuditDto> GetByAdmissionAsync(long admissionId, CancellationToken ct = default)
    {
        var a = await db.FileAudits.AsNoTracking().Include(x => x.AuditedByUser)
            .FirstOrDefaultAsync(x => x.AdmissionId == admissionId, ct) ?? throw new NotFoundException(nameof(FileAudit), admissionId);
        return new FileAuditDto(
            a.Id, a.AdmissionId, a.DischargeSummarySigned, a.ConsentsPresent, a.ProgressNotesComplete,
            a.InvestigationReportsPresent, a.NursingRecordsComplete, a.IsComplete, a.MissingDocumentsNotes,
            a.AuditedByUser!.FullName, a.AuditedAt);
    }
}
