using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Emr;

/// <summary>
/// Drives the patient allergy banner and the OPD prescription warning (plan §11). AllergenName
/// is free text (no drug/allergen master link) — the prescription warning cross-checks it
/// against drug/generic names by substring match, same loose-coupling posture as everywhere
/// else in this codebase. The inherited BaseEntity.IsActive lets a disproven/resolved allergy
/// be deactivated without deleting the safety record — no separate flag needed.
/// </summary>
public class Allergy : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public AllergenType AllergenType { get; set; }
    public required string AllergenName { get; set; }
    public string? Reaction { get; set; }
    public AllergySeverity Severity { get; set; } = AllergySeverity.Moderate;

    public long RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
