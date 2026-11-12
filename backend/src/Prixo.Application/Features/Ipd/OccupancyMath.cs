namespace Prixo.Application.Features.Ipd;

/// <summary>Pure bed-day overlap math for the Ward Occupancy Report — no I/O, so IpReportService delegates here and the rule stays unit-testable without a database.</summary>
public static class OccupancyMath
{
    /// <summary>
    /// Days a bed allocation (<paramref name="allocatedAt"/> to <paramref name="releasedAt"/>, or still
    /// ongoing if null) overlaps the half-open report range [<paramref name="rangeStart"/>,
    /// <paramref name="rangeEndExclusive"/>).
    /// </summary>
    public static decimal OverlapDays(
        DateTimeOffset allocatedAt, DateTimeOffset? releasedAt, DateTimeOffset rangeStart, DateTimeOffset rangeEndExclusive)
    {
        var start = allocatedAt > rangeStart ? allocatedAt : rangeStart;
        var end = releasedAt is { } released && released < rangeEndExclusive ? released : rangeEndExclusive;
        if (end <= start) return 0m;
        return (decimal)(end - start).TotalDays;
    }
}
