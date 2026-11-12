using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Icu;

/// <summary>The subset of an IpVitals reading a CriticalAlertRule can threshold against — no EF/tracking baggage, just the fields the match needs.</summary>
public record VitalsSnapshot(
    decimal? TemperatureF, int? PulseRate, int? RespiratoryRate, int? BloodPressureSystolic, int? BloodPressureDiastolic, int? SpO2);

/// <summary>
/// Pure critical-alert threshold evaluation — no I/O, so IcuVitalsService delegates here and
/// the rule stays unit-testable without a database. Same reasoning as Billing's BillMath /
/// OT's OtMath.
/// </summary>
public static class IcuMath
{
    public static decimal? ExtractValue(CriticalAlertParameter parameter, VitalsSnapshot vitals) => parameter switch
    {
        CriticalAlertParameter.SpO2 => vitals.SpO2,
        CriticalAlertParameter.PulseRate => vitals.PulseRate,
        CriticalAlertParameter.RespiratoryRate => vitals.RespiratoryRate,
        CriticalAlertParameter.BloodPressureSystolic => vitals.BloodPressureSystolic,
        CriticalAlertParameter.BloodPressureDiastolic => vitals.BloodPressureDiastolic,
        CriticalAlertParameter.TemperatureF => vitals.TemperatureF,
        _ => null,
    };

    public static bool IsBreached(ComparisonOperator op, decimal value, decimal threshold) => op switch
    {
        ComparisonOperator.LessThan => value < threshold,
        ComparisonOperator.GreaterThan => value > threshold,
        _ => false,
    };
}
