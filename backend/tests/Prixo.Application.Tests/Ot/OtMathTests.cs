using FluentAssertions;
using Prixo.Application.Features.Ot;

namespace Prixo.Application.Tests.Ot;

public class OtMathOverlapsTests
{
    private static readonly DateTimeOffset Noon = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IdenticalWindows_Overlap() =>
        OtMath.Overlaps(Noon, Noon.AddHours(2), Noon, Noon.AddHours(2)).Should().BeTrue();

    [Fact]
    public void PartialOverlap_Overlaps() =>
        OtMath.Overlaps(Noon, Noon.AddHours(2), Noon.AddHours(1), Noon.AddHours(3)).Should().BeTrue();

    [Fact]
    public void OneWindowInsideAnother_Overlaps() =>
        OtMath.Overlaps(Noon, Noon.AddHours(4), Noon.AddHours(1), Noon.AddHours(2)).Should().BeTrue();

    [Fact]
    public void AdjacentWindows_TouchingAtBoundary_DoNotOverlap() =>
        OtMath.Overlaps(Noon, Noon.AddHours(2), Noon.AddHours(2), Noon.AddHours(4)).Should().BeFalse();

    [Fact]
    public void DisjointWindows_DoNotOverlap() =>
        OtMath.Overlaps(Noon, Noon.AddHours(1), Noon.AddHours(5), Noon.AddHours(6)).Should().BeFalse();

    [Fact]
    public void ReversedArgumentOrder_SameResult() =>
        OtMath.Overlaps(Noon.AddHours(1), Noon.AddHours(3), Noon, Noon.AddHours(2)).Should().BeTrue();
}
