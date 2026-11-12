using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Icu;

public record CriticalAlertRuleDto(
    long Id, CriticalAlertParameter Parameter, ComparisonOperator Operator, decimal ThresholdValue,
    AlertSeverity Severity, string? Description, bool IsActive);

public record SaveCriticalAlertRuleRequest(
    CriticalAlertParameter Parameter, ComparisonOperator Operator, decimal ThresholdValue,
    AlertSeverity Severity, string? Description, bool IsActive = true);
