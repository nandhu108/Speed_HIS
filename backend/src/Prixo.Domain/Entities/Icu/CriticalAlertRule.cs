using Prixo.Domain.Common;
using Prixo.Domain.Enums;

namespace Prixo.Domain.Entities.Icu;

/// <summary>
/// Hospital-configurable vitals threshold (plan §15: "Critical Alert (threshold rules on
/// vitals...)") — branch-scoped so each branch can set its own limits, same "no hardcoded
/// hospital data" posture as Tariffs. IcuVitalsService evaluates every active rule against a
/// newly recorded IpVitals row and raises an IcuVitalsAlert per breach.
/// </summary>
public class CriticalAlertRule : BranchEntity
{
    public CriticalAlertParameter Parameter { get; set; }
    public ComparisonOperator Operator { get; set; }
    public decimal ThresholdValue { get; set; }
    public AlertSeverity Severity { get; set; }
    public string? Description { get; set; }
}
