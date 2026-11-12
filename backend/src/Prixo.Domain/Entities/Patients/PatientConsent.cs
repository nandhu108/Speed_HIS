using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;

namespace Prixo.Domain.Entities.Patients;

/// <summary>A signed consent record captured against a consent template.</summary>
public class PatientConsent : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public long ConsentTemplateId { get; set; }
    public ConsentTemplate? ConsentTemplate { get; set; }

    public required string SignedByName { get; set; }
    /// <summary>Null when the patient signed for themselves.</summary>
    public string? RelationshipToPatient { get; set; }
    public DateTimeOffset SignedAt { get; set; }
    /// <summary>Relative path to the scanned signed copy, if attached.</summary>
    public string? DocumentPath { get; set; }
}
