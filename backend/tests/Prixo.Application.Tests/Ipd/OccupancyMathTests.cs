using FluentAssertions;
using Prixo.Application.Features.Ipd;

namespace Prixo.Application.Tests.Ipd;

public class OccupancyMathTests
{
    private static readonly DateTimeOffset RangeStart = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RangeEnd = new(2026, 7, 8, 0, 0, 0, TimeSpan.Zero); // 7-day range

    [Fact]
    public void AllocationFullyInsideRange_ReturnsItsOwnDuration()
    {
        var allocatedAt = new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero);
        var releasedAt = new DateTimeOffset(2026, 7, 4, 0, 0, 0, TimeSpan.Zero);
        OccupancyMath.OverlapDays(allocatedAt, releasedAt, RangeStart, RangeEnd).Should().Be(2m);
    }

    [Fact]
    public void AllocationSpansWholeRange_ReturnsFullRangeLength()
    {
        var allocatedAt = new DateTimeOffset(2026, 6, 20, 0, 0, 0, TimeSpan.Zero);
        var releasedAt = new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.Zero);
        OccupancyMath.OverlapDays(allocatedAt, releasedAt, RangeStart, RangeEnd).Should().Be(7m);
    }

    [Fact]
    public void AllocatedBeforeRange_ClampsStartToRangeStart()
    {
        var allocatedAt = new DateTimeOffset(2026, 6, 25, 0, 0, 0, TimeSpan.Zero);
        var releasedAt = new DateTimeOffset(2026, 7, 3, 0, 0, 0, TimeSpan.Zero);
        OccupancyMath.OverlapDays(allocatedAt, releasedAt, RangeStart, RangeEnd).Should().Be(2m);
    }

    [Fact]
    public void ReleasedAfterRange_ClampsEndToRangeEnd()
    {
        var allocatedAt = new DateTimeOffset(2026, 7, 6, 0, 0, 0, TimeSpan.Zero);
        var releasedAt = new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero);
        OccupancyMath.OverlapDays(allocatedAt, releasedAt, RangeStart, RangeEnd).Should().Be(2m);
    }

    [Fact]
    public void StillOccupied_TreatsOpenEndAsRangeEnd()
    {
        var allocatedAt = new DateTimeOffset(2026, 7, 5, 0, 0, 0, TimeSpan.Zero);
        OccupancyMath.OverlapDays(allocatedAt, null, RangeStart, RangeEnd).Should().Be(3m);
    }

    [Fact]
    public void NoOverlap_ReturnsZero()
    {
        var allocatedAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var releasedAt = new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);
        OccupancyMath.OverlapDays(allocatedAt, releasedAt, RangeStart, RangeEnd).Should().Be(0m);
    }
}
