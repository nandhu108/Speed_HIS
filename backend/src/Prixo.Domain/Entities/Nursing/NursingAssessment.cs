using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;

namespace Prixo.Domain.Entities.Nursing;

/// <summary>
/// Structured nursing assessment (plan §10 "admission assessment, structured jsonb form
/// templates"). TemplateCode is free-text like IntakeOutputEntry.Category — no template
/// master yet, so the frontend owns the form shape per code and DataJson carries the answers.
/// </summary>
public class NursingAssessment : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public required string TemplateCode { get; set; }
    public DateTimeOffset AssessedAt { get; set; }
    public long AssessedByUserId { get; set; }
    public User? AssessedByUser { get; set; }

    /// <summary>jsonb — the template's answers, shape owned by TemplateCode on the frontend.</summary>
    public required string DataJson { get; set; }
}
