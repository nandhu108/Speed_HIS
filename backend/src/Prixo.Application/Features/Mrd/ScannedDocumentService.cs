using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Mrd;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Mrd;

/// <summary>
/// Document Scanning (plan §17). Same on-disk storage idiom as EMR's Attachment/Phase 3's
/// PatientDocument, scoped to an admission. VirusScanStatus stays NotScanned — see IVirusScanner.
/// </summary>
public class ScannedDocumentService(IAppDbContext db, IFileStorageService files, ICurrentUser currentUser, TimeProvider timeProvider)
{
    private const long MaxDocumentBytes = 20 * 1024 * 1024;

    public async Task<long> UploadAsync(
        long admissionId, long documentTypeId, Stream content, string fileName, string contentType,
        long sizeBytes, string? description, CancellationToken ct = default)
    {
        if (sizeBytes > MaxDocumentBytes)
        {
            throw new DomainException("File must be under 20 MB.");
        }
        var admission = await db.Admissions.FirstOrDefaultAsync(a => a.Id == admissionId, ct)
            ?? throw new NotFoundException(nameof(Admission), admissionId);
        if (!await db.RecordDocumentTypes.AnyAsync(t => t.Id == documentTypeId, ct))
        {
            throw new NotFoundException("Document type", documentTypeId);
        }

        var stored = await files.SaveAsync(content, $"mrd-documents/{admission.AdmissionNumber}", fileName, ct);
        var document = new ScannedDocument
        {
            AdmissionId = admissionId, DocumentTypeId = documentTypeId, FileName = fileName, FilePath = stored.RelativePath,
            ContentType = contentType, SizeBytes = stored.SizeBytes, Description = description,
            UploadedByUserId = currentUser.UserId, UploadedAt = timeProvider.GetUtcNow(),
        };
        db.ScannedDocuments.Add(document);
        await db.SaveChangesAsync(ct);
        return document.Id;
    }

    public async Task<IReadOnlyList<ScannedDocumentDto>> GetByAdmissionAsync(long admissionId, CancellationToken ct = default) =>
        await db.ScannedDocuments.AsNoTracking().Where(d => d.AdmissionId == admissionId)
            .Include(d => d.DocumentType).Include(d => d.UploadedByUser)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new ScannedDocumentDto(
                d.Id, d.AdmissionId, d.DocumentTypeId, d.DocumentType!.Name, d.FileName, d.ContentType, d.SizeBytes,
                d.Description, d.VirusScanStatus, d.UploadedByUser!.FullName, d.UploadedAt))
            .ToListAsync(ct);

    public async Task<(Stream Content, string ContentType, string FileName)> OpenAsync(long admissionId, long documentId, CancellationToken ct = default)
    {
        var document = await db.ScannedDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.AdmissionId == admissionId, ct)
            ?? throw new NotFoundException(nameof(ScannedDocument), documentId);
        var stream = await files.OpenReadAsync(document.FilePath, ct);
        return (stream, document.ContentType, document.FileName);
    }
}
