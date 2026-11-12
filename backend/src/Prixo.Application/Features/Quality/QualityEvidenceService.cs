using Microsoft.EntityFrameworkCore;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Interfaces;
using Prixo.Domain.Entities.Quality;
using Prixo.Domain.Exceptions;

namespace Prixo.Application.Features.Quality;

/// <summary>NABH Evidence Upload (plan §20: "chapter/standard-wise document repository"). Same on-disk storage idiom as MRD's ScannedDocument / Biomed's Calibration certificate.</summary>
public class QualityEvidenceService(IAppDbContext db, ICurrentUser currentUser, IFileStorageService files, TimeProvider timeProvider)
{
    public async Task<long> UploadAsync(UploadEvidenceRequest request, Stream content, string fileName, CancellationToken ct = default)
    {
        var standard = await db.QualityNabhStandards.FirstOrDefaultAsync(s => s.Id == request.StandardId, ct)
            ?? throw new NotFoundException(nameof(QualityNabhStandard), request.StandardId);

        var stored = await files.SaveAsync(content, $"quality-evidence/{standard.Code}", fileName, ct);
        var evidence = new QualityEvidence
        {
            BranchId = RequireBranch(), StandardId = request.StandardId, Title = request.Title, Description = request.Description,
            FilePath = stored.RelativePath, FileName = fileName, UploadedByUserId = currentUser.UserId,
            UploadedAt = timeProvider.GetUtcNow(),
        };
        db.QualityEvidence.Add(evidence);
        await db.SaveChangesAsync(ct);
        return evidence.Id;
    }

    public async Task<IReadOnlyList<EvidenceDto>> GetAllAsync(EvidenceQuery query, CancellationToken ct = default)
    {
        var items = db.QualityEvidence.AsNoTracking().Include(e => e.Standard).Include(e => e.UploadedByUser).AsQueryable();
        if (query.StandardId is { } standardId) items = items.Where(e => e.StandardId == standardId);
        var rows = await items.OrderByDescending(e => e.UploadedAt).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<(Stream Content, string FileName)> OpenAsync(long id, CancellationToken ct = default)
    {
        var evidence = await db.QualityEvidence.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException(nameof(QualityEvidence), id);
        var stream = await files.OpenReadAsync(evidence.FilePath, ct);
        return (stream, evidence.FileName);
    }

    private static EvidenceDto ToDto(QualityEvidence e) => new(
        e.Id, e.StandardId, e.Standard!.Code, e.Title, e.Description, e.FileName, e.UploadedByUser!.FullName, e.UploadedAt);

    private long RequireBranch() => currentUser.BranchId ?? throw new DomainException("Select a branch first.");
}
