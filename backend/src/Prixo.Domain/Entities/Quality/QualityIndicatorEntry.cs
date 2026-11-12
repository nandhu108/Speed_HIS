using Prixo.Domain.Common;
using Prixo.Domain.Entities.Admin;

namespace Prixo.Domain.Entities.Quality;

/// <summary>
/// One monthly data point for an indicator (plan §20: "monthly data entry / auto-pull where
/// computable"). ComputedValue is deliberately not stored — QualityMath.ComputedValue derives
/// it from Numerator/Denominator on read, same "compute on read" posture as Biomed's
/// IsRenewalDue/DowntimeMinutes.
/// </summary>
public class QualityIndicatorEntry : BranchEntity
{
    public long IndicatorId { get; set; }
    public QualityIndicator? Indicator { get; set; }

    /// <summary>Normalized to the first day of the month.</summary>
    public DateOnly PeriodMonth { get; set; }

    public decimal Numerator { get; set; }
    public decimal Denominator { get; set; }
    public bool IsAutoComputed { get; set; }
    public string? Remarks { get; set; }

    public long EnteredByUserId { get; set; }
    public User? EnteredByUser { get; set; }
}
