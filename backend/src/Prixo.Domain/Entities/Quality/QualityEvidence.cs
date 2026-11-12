using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;

namespace Prixo.Domain.Entities.Quality;

/// <summary>NABH Evidence Upload (plan §20: "chapter/standard-wise document repository"). File stored via IFileStorageService, same idiom as MRD's ScannedDocument / Biomed's Calibration certificate.</summary>
public class QualityEvidence : BranchEntity
{
    public long StandardId { get; set; }
    public QualityNabhStandard? Standard { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }

    public required string FilePath { get; set; }
    public required string FileName { get; set; }

    public long UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
