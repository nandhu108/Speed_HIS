using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Nursing;

/// <summary>
/// Pain / Morse Fall Risk / Braden Bed Sore scales (plan §10 "score auto-calc"). ScoreDataJson
/// holds the raw per-item scores the frontend submitted; TotalScore/RiskLevel are computed
/// server-side by NursingMath so the banding rule lives in one testable place.
/// </summary>
public class RiskAssessment : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public RiskAssessmentType AssessmentType { get; set; }
    public DateTimeOffset AssessedAt { get; set; }
    public long AssessedByUserId { get; set; }
    public User? AssessedByUser { get; set; }

    /// <summary>jsonb — raw per-item scores keyed by item code.</summary>
    public required string ScoreDataJson { get; set; }
    public int TotalScore { get; set; }
    public required string RiskLevel { get; set; }
}
