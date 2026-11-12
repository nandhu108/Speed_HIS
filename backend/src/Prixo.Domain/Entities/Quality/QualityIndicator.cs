using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Quality;

/// <summary>
/// Quality Indicators master (plan §20: "numerator/denominator definitions"). Global, like the
/// Lab test catalog — a hospital's indicator set doesn't vary per branch, only its monthly
/// <see cref="QualityIndicatorEntry"/> data does. AutoComputeSource is null for a manual-entry
/// indicator; set, it lets QualityIndicatorService pull the numerator/denominator from an
/// existing engine (Lab TAT, bed occupancy, incident count) instead of typing it in.
/// </summary>
public class QualityIndicator : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public required string NumeratorDefinition { get; set; }
    public required string DenominatorDefinition { get; set; }
    public string? Unit { get; set; }
    public decimal? TargetValue { get; set; }
    public QualityAutoComputeSource? AutoComputeSource { get; set; }
}
