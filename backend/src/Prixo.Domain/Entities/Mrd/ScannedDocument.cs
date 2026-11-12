using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Mrd;

/// <summary>
/// Document Scanning (plan §17: "upload against admission, document-type master"). Same
/// on-disk storage idiom as EMR's Attachment/Phase 3's PatientDocument — a distinct table
/// because scope here is specifically the MRD file, not the whole patient. VirusScanStatus
/// is the same placeholder column; see IVirusScanner.
/// </summary>
public class ScannedDocument : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public long DocumentTypeId { get; set; }
    public RecordDocumentType? DocumentType { get; set; }

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
