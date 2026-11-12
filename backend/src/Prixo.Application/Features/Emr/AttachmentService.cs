using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Emr;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Emr;

/// <summary>
/// External reports / scanned documents (plan §11). Same on-disk storage service Phase 3's
/// PatientDocument uses, just a different subfolder and a distinct table — Attachment is
/// clinical-context ("Lab Report", "Referral Letter"...), PatientDocument is registration-time
/// (ID proof/photo/consent scan). VirusScanStatus stays NotScanned — see IVirusScanner.
/// </summary>
public class AttachmentService(IAppDbContext db, IFileStorageService files, ICurrentUser currentUser, TimeProvider timeProvider)
{
    private const long MaxAttachmentBytes = 20 * 1024 * 1024;

    public async Task<long> UploadAsync(
        long patientId, string attachmentType, Stream content, string fileName, string contentType,
        long sizeBytes, string? description, CancellationToken ct = default)
    {
        if (sizeBytes > MaxAttachmentBytes)
        {
            throw new DomainException("File must be under 20 MB.");
        }
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == patientId, ct)
            ?? throw new NotFoundException(nameof(Patient), patientId);

        var stored = await files.SaveAsync(content, $"emr-attachments/{patient.Uhid}", fileName, ct);
        var attachment = new Attachment
        {
            PatientId = patientId, AttachmentType = attachmentType, FileName = fileName, FilePath = stored.RelativePath,
            ContentType = contentType, SizeBytes = stored.SizeBytes, Description = description,
            UploadedByUserId = currentUser.UserId, UploadedAt = timeProvider.GetUtcNow(),
        };
        db.Attachments.Add(attachment);
        await db.SaveChangesAsync(ct);
        return attachment.Id;
    }

    public async Task<IReadOnlyList<AttachmentDto>> GetAsync(long patientId, CancellationToken ct = default) =>
        await db.Attachments.AsNoTracking().Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.UploadedAt)
            .Join(db.Users.AsNoTracking(), a => a.UploadedByUserId, u => u.Id, (a, u) =>
                new AttachmentDto(a.Id, a.AttachmentType, a.FileName, a.ContentType, a.SizeBytes, a.Description, a.VirusScanStatus, u.FullName, a.UploadedAt))
            .ToListAsync(ct);

    public async Task<(Stream Content, string ContentType, string FileName)> OpenAsync(long patientId, long attachmentId, CancellationToken ct = default)
    {
        var attachment = await db.Attachments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.PatientId == patientId, ct)
            ?? throw new NotFoundException(nameof(Attachment), attachmentId);
        var stream = await files.OpenReadAsync(attachment.FilePath, ct);
        return (stream, attachment.ContentType, attachment.FileName);
    }
}
