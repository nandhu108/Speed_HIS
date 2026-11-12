using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Patients;

namespace Prixo.Domain.Entities.Emr;

public class FamilyHistory : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    /// <summary>Free text — "Father", "Mother", "Sibling"… no relation master.</summary>
    public required string Relation { get; set; }
    public required string Condition { get; set; }
    public string? Notes { get; set; }

    public long RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
