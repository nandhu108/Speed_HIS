using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Insurance;

/// <summary>Claim supporting-document upload — mirrors Mrd.ScannedDocument's shape.</summary>
public class ClaimDocument : BaseEntity
{
    public long ClaimId { get; set; }
    public Claim? Claim { get; set; }

    public long DocumentTypeId { get; set; }
    public ClaimDocumentType? DocumentType { get; set; }

    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }

    /// <summary>Placeholder per plan §11 — stays NotScanned until a real scanner integration lands.</summary>
    public VirusScanStatus VirusScanStatus { get; set; } = VirusScanStatus.NotScanned;

    public long UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
