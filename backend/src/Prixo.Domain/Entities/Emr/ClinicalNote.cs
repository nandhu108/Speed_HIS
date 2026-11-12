using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Entities.Opd;
using Prixo.Domain.Entities.Patients;

namespace Prixo.Domain.Entities.Emr;

/// <summary>
/// Ad-hoc clinical note (plan §11 "specialty templates"). NoteType is free text (e.g.
/// "Cardiology", "General") — no specialty-template master, the frontend owns any per-type
/// prompts. OpVisitId/AdmissionId are both optional, loose context links (a note can stand
/// alone, or be tied back to the visit/admission it was written during) — same posture as
/// IPD's ReferringOpVisitId.
/// </summary>
public class ClinicalNote : BaseEntity
{
    public long PatientId { get; set; }
    public Patient? Patient { get; set; }

    public required string NoteType { get; set; }
    public string? Title { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset NoteAt { get; set; }

    public long RecordedByUserId { get; set; }
    public User? RecordedByUser { get; set; }

    public long? OpVisitId { get; set; }
    public OpVisit? OpVisit { get; set; }
    public long? AdmissionId { get; set; }
    public Admission? Admission { get; set; }
}
