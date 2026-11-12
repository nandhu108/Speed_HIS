using FluentAssertions;
using Prixo.Application.Features.Icu;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Icu;

public class IcuMathExtractValueTests
{
    private static readonly VitalsSnapshot Snapshot = new(98.6m, 88, 18, 120, 80, 96);

    [Fact]
    public void SpO2_Extracted() => IcuMath.ExtractValue(CriticalAlertParameter.SpO2, Snapshot).Should().Be(96);

    [Fact]
    public void PulseRate_Extracted() => IcuMath.ExtractValue(CriticalAlertParameter.PulseRate, Snapshot).Should().Be(88);

    [Fact]
    public void MissingValue_ReturnsNull() =>
        IcuMath.ExtractValue(CriticalAlertParameter.SpO2, new VitalsSnapshot(null, null, null, null, null, null)).Should().BeNull();
}

public class IcuMathIsBreachedTests
{
    [Fact]
    public void LessThan_BelowThreshold_IsBreach() => IcuMath.IsBreached(ComparisonOperator.LessThan, 88, 90).Should().BeTrue();

    [Fact]
    public void LessThan_AtThreshold_NotBreach() => IcuMath.IsBreached(ComparisonOperator.LessThan, 90, 90).Should().BeFalse();

    [Fact]
    public void LessThan_AboveThreshold_NotBreach() => IcuMath.IsBreached(ComparisonOperator.LessThan, 95, 90).Should().BeFalse();

    [Fact]
    public void GreaterThan_AboveThreshold_IsBreach() => IcuMath.IsBreached(ComparisonOperator.GreaterThan, 135, 130).Should().BeTrue();

    [Fact]
    public void GreaterThan_AtThreshold_NotBreach() => IcuMath.IsBreached(ComparisonOperator.GreaterThan, 130, 130).Should().BeFalse();

    [Fact]
    public void GreaterThan_BelowThreshold_NotBreach() => IcuMath.IsBreached(ComparisonOperator.GreaterThan, 100, 130).Should().BeFalse();
}
