using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Icu;

/// <summary>A point-in-time acuity call by the treating team — not a computed score (no APACHE II/SOFA in this phase, just a clinician-assigned level), giving the dashboard a severity history per stay.</summary>
public class IcuSeverityAssessment : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public DateTimeOffset AssessedAt { get; set; }
    public long AssessedByUserId { get; set; }
    public User? AssessedByUser { get; set; }
    public IcuSeverityLevel SeverityLevel { get; set; }
    public string? Notes { get; set; }
}
