using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Emr;

/// <summary>
/// External reports / scanned documents (plan §11) — distinct from Phase 3's PatientDocument
/// (registration-time ID proof/photo/consent scan). AttachmentType is free text (e.g. "Lab
/// Report", "Referral Letter") — no document-type master. VirusScanStatus is a placeholder
/// column per plan ("virus-scan hook placeholder"); see IVirusScanner — nothing writes
/// anything but NotScanned yet.
/// </summary>
public class Attachment : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public required string AttachmentType { get; set; }
    public required string FileName { get; set; }
    /// <summary>Relative path under the file storage root — never a raw filesystem path to a client.</summary>
    public required string FilePath { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string? Description { get; set; }
    public VirusScanStatus VirusScanStatus { get; set; } = VirusScanStatus.NotScanned;

    public long UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
