using Prixo.Domain.Common;
using Prixo.Domain.Entities.Masters;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Er;

/// <summary>
/// The "fast clinical sheet" (plan §16) — one row per visit, find-or-create upserted as the
/// case progresses, same posture as OT's OperativeNote. Disposition carries the case to its
/// conclusion; TransferredToFacility/TransferSummary only apply when Disposition is
/// TransferredOut (plan's "Referral / Transfer Out (with transfer summary)") — folded onto
/// this row rather than a separate table since they're just extra detail on the same
/// decision, not an independent concern.
/// </summary>
public class EmergencyAssessment : BaseEntity
{
    public long EmergencyVisitId { get; set; }
    public EmergencyVisit? EmergencyVisit { get; set; }

    public long AssessedByDoctorId { get; set; }
    public Doctor? AssessedByDoctor { get; set; }
    public DateTimeOffset AssessedAt { get; set; }

    public string? History { get; set; }
    public string? ExaminationFindings { get; set; }
    public string? ProvisionalDiagnosis { get; set; }
    public string? TreatmentGiven { get; set; }

    public DispositionType? Disposition { get; set; }
    public string? DispositionNotes { get; set; }
    public string? TransferredToFacility { get; set; }
    public string? TransferSummary { get; set; }
}
