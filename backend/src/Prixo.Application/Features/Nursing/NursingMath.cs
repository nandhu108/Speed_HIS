using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Nursing;

/// <summary>
/// Pure MAR due-time generation and risk-score banding — no I/O, so MarService/NursingChartService
/// delegate here and the rules stay unit-testable without a database. Same reasoning as
/// Billing's BillMath / Lab's LabMath / Pharmacy's StockMath / IPD's IpdMath.
/// </summary>
public static class NursingMath
{
    /// <summary>
    /// Fixed clock times per frequency code — no drug-frequency master exists (plan §9's
    /// MedicationOrder.Frequency is free text), so this is a small, deliberately incomplete
    /// lookup covering the common Indian-hospital shorthand. Unrecognized codes and SOS
    /// ("as needed" — no fixed schedule) generate nothing; STAT generates one immediate dose.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, TimeOnly[]> FrequencyTimes = new Dictionary<string, TimeOnly[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["OD"] = [new TimeOnly(8, 0)],
        ["HS"] = [new TimeOnly(21, 0)],
        ["BD"] = [new TimeOnly(8, 0), new TimeOnly(20, 0)],
        ["BID"] = [new TimeOnly(8, 0), new TimeOnly(20, 0)],
        ["TID"] = [new TimeOnly(8, 0), new TimeOnly(14, 0), new TimeOnly(20, 0)],
        ["TDS"] = [new TimeOnly(8, 0), new TimeOnly(14, 0), new TimeOnly(20, 0)],
        ["QID"] = [new TimeOnly(8, 0), new TimeOnly(14, 0), new TimeOnly(20, 0), new TimeOnly(2, 0)],
        ["QDS"] = [new TimeOnly(8, 0), new TimeOnly(14, 0), new TimeOnly(20, 0), new TimeOnly(2, 0)],
        ["Q6H"] = [new TimeOnly(6, 0), new TimeOnly(12, 0), new TimeOnly(18, 0), new TimeOnly(0, 0)],
        ["Q8H"] = [new TimeOnly(8, 0), new TimeOnly(16, 0), new TimeOnly(0, 0)],
        ["Q12H"] = [new TimeOnly(8, 0), new TimeOnly(20, 0)],
    };

    /// <summary>
    /// Due-time rows for a single calendar day, clipped to the order's StartDate/EndDate.
    /// SOS (as-needed) and unrecognized frequency codes yield no rows — administration for
    /// those is recorded ad hoc against the order itself, not a generated schedule.
    /// STAT yields exactly one due time: the moment the order was placed.
    /// </summary>
    public static IReadOnlyList<DateTimeOffset> GenerateDueTimes(
        string frequency, DateOnly startDate, DateOnly? endDate, DateOnly forDate, DateTimeOffset orderedAt)
    {
        if (forDate < startDate || (endDate is { } end && forDate > end))
        {
            return [];
        }
        if (string.Equals(frequency, "SOS", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }
        if (string.Equals(frequency, "STAT", StringComparison.OrdinalIgnoreCase))
        {
            return forDate == DateOnly.FromDateTime(orderedAt.UtcDateTime) ? [orderedAt] : [];
        }
        if (!FrequencyTimes.TryGetValue(frequency.Trim(), out var times))
        {
            return [];
        }
        return [.. times.Select(t => new DateTimeOffset(forDate.ToDateTime(t), TimeSpan.Zero)).OrderBy(t => t)];
    }

    /// <summary>Sums the raw per-item scores a caller collected; banding is type-specific below.</summary>
    public static int SumScore(IEnumerable<int> itemScores) => itemScores.Sum();

    public static string ResolveRiskLevel(RiskAssessmentType type, int totalScore) => type switch
    {
        RiskAssessmentType.Pain => totalScore switch
        {
            0 => "None",
            <= 3 => "Mild",
            <= 6 => "Moderate",
            _ => "Severe",
        },
        RiskAssessmentType.FallRisk => totalScore switch // Morse Fall Scale, 0-125
        {
            < 25 => "Low",
            < 45 => "Moderate",
            _ => "High",
        },
        RiskAssessmentType.BedSore => totalScore switch // Braden Scale, 6-23 (lower = worse)
        {
            <= 9 => "Severe",
            <= 12 => "High",
            <= 14 => "Moderate",
            <= 18 => "Mild",
            _ => "None",
        },
        _ => "Unknown",
    };
}
