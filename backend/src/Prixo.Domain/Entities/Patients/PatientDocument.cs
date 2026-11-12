using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Patients;

/// <summary>Uploaded file attached to a patient (photo, ID proof, signed consent scan…).</summary>
public class PatientDocument : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public PatientDocumentType DocumentType { get; set; }
    public required string FileName { get; set; }
    /// <summary>Relative path under the file storage root — never a raw filesystem path to a client.</summary>
    public required string FilePath { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string? Description { get; set; }
}
