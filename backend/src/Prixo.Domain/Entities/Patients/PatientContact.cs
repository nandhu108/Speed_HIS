using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Patients;

/// <summary>Guardian, attender or emergency contact for a patient.</summary>
public class PatientContact : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public PatientContactType ContactType { get; set; }
    public required string Name { get; set; }
    public string? Relationship { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsPrimary { get; set; }
}
