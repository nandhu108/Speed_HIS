using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Patients;

namespace Prixo.Domain.Entities.Emr;

public class SurgicalHistory : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public required string ProcedureName { get; set; }
    public DateOnly? ProcedureDate { get; set; }
    /// <summary>Free text — the surgeon/hospital, not necessarily a Doctor master row (could be an external procedure).</summary>
    public string? Surgeon { get; set; }
    public string? Notes { get; set; }

    public long RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
