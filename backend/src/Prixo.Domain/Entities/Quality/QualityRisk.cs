using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Quality;

/// <summary>Risk Register (plan §20). RiskScore (Likelihood x Impact) is computed on read via QualityMath, not stored.</summary>
public class QualityRisk : BranchEntity
{
    public required string Title { get; set; }
    public string? Category { get; set; }
    public required string Description { get; set; }

    /// <summary>1-5.</summary>
    public int Likelihood { get; set; }
    /// <summary>1-5.</summary>
    public int Impact { get; set; }

    public string? MitigationPlan { get; set; }

    public long OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }

    public RiskStatus Status { get; set; } = RiskStatus.Identified;
    public DateOnly IdentifiedDate { get; set; }
    public DateOnly? ReviewDate { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}
