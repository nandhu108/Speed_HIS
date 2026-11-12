using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Patients;

namespace Prixo.Domain.Entities.Emr;

public class Immunization : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public required string VaccineName { get; set; }
    public int? DoseNumber { get; set; }
    public DateOnly AdministeredDate { get; set; }
    public DateOnly? NextDueDate { get; set; }
    /// <summary>Free text — administering clinician/facility, not necessarily this hospital.</summary>
    public string? AdministeredBy { get; set; }
    public string? Notes { get; set; }

    public long RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
