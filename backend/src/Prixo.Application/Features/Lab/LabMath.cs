using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Lab;

/// <summary>A reference-range row with no EF/tracking baggage, just the fields resolution needs.</summary>
public record ReferenceRangeCandidate(
    long Id, Gender? Sex, int? MinAgeYears, int? MaxAgeYears,
    decimal? NormalLow, decimal? NormalHigh, decimal? CriticalLow, decimal? CriticalHigh, string? TextNormalValue);

/// <summary>
/// Pure age calculation and reference-range/flag selection — no I/O, so LabOrderService
/// delegates here and the rules stay unit-testable without a database. Same reasoning as
/// Billing's BillMath.
/// </summary>
public static class LabMath
{
    public static int? CalculateAgeYears(DateOnly? dateOfBirth, int? estimatedAge, DateOnly asOf)
    {
        if (dateOfBirth is not { } dob)
        {
            return estimatedAge;
        }
        var years = asOf.Year - dob.Year;
        if (dob > asOf.AddYears(-years))
        {
            years--;
        }
        return years;
    }

    /// <summary>Prefers a sex-specific row over sex-agnostic, then an age-bounded row over unbounded.</summary>
    public static ReferenceRangeCandidate? ResolveRange(
        IEnumerable<ReferenceRangeCandidate> candidates, Gender patientSex, int? ageYears)
    {
        return candidates
            .Where(c => (c.Sex == null || c.Sex == patientSex)
                && (c.MinAgeYears == null || ageYears == null || ageYears >= c.MinAgeYears)
                && (c.MaxAgeYears == null || ageYears == null || ageYears <= c.MaxAgeYears))
            .OrderByDescending(c => c.Sex != null)
            .ThenByDescending(c => c.MinAgeYears != null || c.MaxAgeYears != null)
            .FirstOrDefault();
    }

    /// <summary>Critical bounds take precedence over normal bounds. No range resolved means Normal (nothing to flag against).</summary>
    public static ResultFlag ResolveFlag(decimal value, ReferenceRangeCandidate? range)
    {
        if (range is null)
        {
            return ResultFlag.Normal;
        }
        if (range.CriticalLow is { } criticalLow && value < criticalLow)
        {
            return ResultFlag.Critical;
        }
        if (range.CriticalHigh is { } criticalHigh && value > criticalHigh)
        {
            return ResultFlag.Critical;
        }
        if (range.NormalLow is { } normalLow && value < normalLow)
        {
            return ResultFlag.Low;
        }
        if (range.NormalHigh is { } normalHigh && value > normalHigh)
        {
            return ResultFlag.High;
        }
        return ResultFlag.Normal;
    }

    /// <summary>
    /// Order-level rollup from item statuses — same recompute-from-children idiom as
    /// Billing's RecomputeBillTotals. Cancelled items are excluded from the
    /// all-approved/all-cancelled checks except when literally every item is cancelled.
    /// </summary>
    public static LabOrderStatus RecomputeOrderStatus(IReadOnlyCollection<LabOrderItemStatus> itemStatuses)
    {
        if (itemStatuses.Count == 0 || itemStatuses.All(s => s == LabOrderItemStatus.Cancelled))
        {
            return LabOrderStatus.Cancelled;
        }
        var relevant = itemStatuses.Where(s => s != LabOrderItemStatus.Cancelled).ToList();
        if (relevant.All(s => s == LabOrderItemStatus.Approved))
        {
            return LabOrderStatus.Completed;
        }
        return itemStatuses.Any(s => s != LabOrderItemStatus.Ordered)
            ? LabOrderStatus.InProgress
            : LabOrderStatus.Ordered;
    }
}
