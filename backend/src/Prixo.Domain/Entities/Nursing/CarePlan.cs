using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Entities.Ipd;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Nursing;

public class CarePlan : BaseEntity
{
    public long AdmissionId { get; set; }
    public Admission? Admission { get; set; }

    public required string ProblemStatement { get; set; }
    public required string Goal { get; set; }
    public required string Interventions { get; set; }
    public CarePlanStatus Status { get; set; } = CarePlanStatus.Active;

    public long CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public string? EvaluationNotes { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}
