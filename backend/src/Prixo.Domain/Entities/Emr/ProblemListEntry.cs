using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Emr;

/// <summary>
/// The diagnosis registry (plan §11) — a patient-level, manually-curated Active/Resolved
/// problem list, distinct from OPD's per-visit VisitDiagnosis (which has no status and isn't
/// touched by this table). IcdCodeId is optional; DiagnosisText is always snapshotted so the
/// entry still reads correctly if the ICD row is edited or the code is left unlinked.
/// </summary>
public class ProblemListEntry : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public long? IcdCodeId { get; set; }
    public IcdCode? IcdCode { get; set; }
    public required string DiagnosisText { get; set; }

    public ProblemStatus Status { get; set; } = ProblemStatus.Active;
    public DateOnly IdentifiedDate { get; set; }
    public DateOnly? ResolvedDate { get; set; }
    public string? Notes { get; set; }

    public long RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }
}
