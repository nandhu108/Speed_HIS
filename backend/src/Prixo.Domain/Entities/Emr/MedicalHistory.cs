using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Patients;

namespace Prixo.Domain.Entities.Emr;

/// <summary>
/// Patient-level clinical history (plan §11). Global like Patient itself — not branch-scoped —
/// because clinical safety data must read the same regardless of which branch a doctor is
/// viewing the patient from.
/// </summary>
public class MedicalHistory : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public required string Condition { get; set; }
    public DateOnly? DiagnosedDate { get; set; }
    public string? Notes { get; set; }

    public long RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
