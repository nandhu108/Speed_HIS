namespace Prixo.Application.Features.Ipd;

/// <summary>A room-type-charge row candidate for rate resolution — no EF/tracking baggage, just the fields the match needs.</summary>
public record RoomTypeChargeCandidate(DateOnly EffectiveFrom, DateOnly? EffectiveTo, decimal Rate);

/// <summary>
/// Pure bed-day and age-display math — no I/O, so AdmissionService/BillingService delegate
/// here and the rules stay unit-testable without a database. Same reasoning as Billing's
/// BillMath / Lab's LabMath / Pharmacy's StockMath.
/// </summary>
public static class IpdMath
{
    /// <summary>Newest RoomTypeCharge row effective as of <paramref name="asOf"/>; falls back to the RoomType's own flat BedChargePerDay if no row is effective (e.g. no charge history has been entered yet).</summary>
    public static decimal ResolveRoomTypeCharge(IEnumerable<RoomTypeChargeCandidate> candidates, DateOnly asOf, decimal fallbackRate)
    {
        var match = candidates
            .Where(c => c.EffectiveFrom <= asOf && (c.EffectiveTo == null || c.EffectiveTo >= asOf))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefault();
        return match?.Rate ?? fallbackRate;
    }

    /// <summary>
    /// Whole calendar days a bed was occupied for a single allocation segment, billed like a
    /// hotel stay: any part of a day counts as a full day, minimum one day even for a
    /// same-day transfer/discharge.
    /// </summary>
    public static int ComputeBedDays(DateTimeOffset allocatedAt, DateTimeOffset asOf)
    {
        if (asOf <= allocatedAt)
        {
            return 1;
        }
        return Math.Max((int)Math.Ceiling((asOf - allocatedAt).TotalDays), 1);
    }

    public static string FormatAge(DateOnly? dateOfBirth, int? estimatedAge, DateOnly asOf)
    {
        if (dateOfBirth is { } dob)
        {
            var years = asOf.Year - dob.Year;
            if (dob > asOf.AddYears(-years))
            {
                years--;
            }
            if (years >= 2)
            {
                return $"{years} yr(s)";
            }
            var months = ((asOf.Year - dob.Year) * 12) + asOf.Month - dob.Month - (asOf.Day < dob.Day ? 1 : 0);
            return $"{Math.Max(months, 0)} mo(s)";
        }
        return estimatedAge is { } age ? $"~{age} yr(s)" : "Unknown";
    }

    /// <summary>
    /// Whole completed years as of <paramref name="asOf"/> — the numeric counterpart to
    /// <see cref="FormatAge"/>, for callers that need to compare against a threshold (e.g. the
    /// OPIP Census Report's configurable "Child" age cutoff) rather than display a string.
    /// Null when neither a birth date nor an estimated age is on file.
    /// </summary>
    public static int? ComputeAgeYears(DateOnly? dateOfBirth, int? estimatedAge, DateOnly asOf)
    {
        if (dateOfBirth is { } dob)
        {
            var years = asOf.Year - dob.Year;
            if (dob > asOf.AddYears(-years))
            {
                years--;
            }
            return Math.Max(years, 0);
        }
        return estimatedAge;
    }
}
