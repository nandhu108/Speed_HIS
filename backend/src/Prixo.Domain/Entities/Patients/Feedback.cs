using Prixo.Domain.Common;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Opd;

namespace Prixo.Domain.Entities.Patients;

/// <summary>
/// Portal Feedback (plan §21: "visit-linked ratings → Quality module"). Lives in the
/// `patient` schema rather than `quality.feedbacks` — Quality (Phase 20) is a held module
/// for now, so this is the same "schema-ready placeholder" posture as the ABHA/Insurance
/// columns elsewhere: real data captured today, migrates onto quality.feedbacks once that
/// module exists. Exactly one of OpVisitId/AdmissionId is set.
/// </summary>
public class Feedback : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }
    public long? OpVisitId { get; set; }
    public OpVisit? OpVisit { get; set; }
    public long? AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public int Rating { get; set; }
    public string? Comments { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}
