using FluentAssertions;
using Prixo.Application.Features.Ipd;

namespace Prixo.Application.Tests.Ipd;

public class IpdMathComputeBedDaysTests
{
    [Fact]
    public void SameInstant_CountsAsOneDay()
    {
        var now = new DateTimeOffset(2027, 1, 1, 10, 0, 0, TimeSpan.Zero);
        IpdMath.ComputeBedDays(now, now).Should().Be(1);
    }

    [Fact]
    public void PartialDay_RoundsUpToOneDay()
    {
        var allocatedAt = new DateTimeOffset(2027, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var asOf = allocatedAt.AddHours(5);
        IpdMath.ComputeBedDays(allocatedAt, asOf).Should().Be(1);
    }

    [Fact]
    public void ExactlyTwoDays_ReturnsTwo()
    {
        var allocatedAt = new DateTimeOffset(2027, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var asOf = allocatedAt.AddDays(2);
        IpdMath.ComputeBedDays(allocatedAt, asOf).Should().Be(2);
    }

    [Fact]
    public void PartOfThirdDay_RoundsUpToThree()
    {
        var allocatedAt = new DateTimeOffset(2027, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var asOf = allocatedAt.AddDays(2).AddHours(1);
        IpdMath.ComputeBedDays(allocatedAt, asOf).Should().Be(3);
    }

    [Fact]
    public void AsOfBeforeAllocation_StillReturnsMinimumOneDay()
    {
        var allocatedAt = new DateTimeOffset(2027, 1, 2, 10, 0, 0, TimeSpan.Zero);
        var asOf = allocatedAt.AddHours(-1);
        IpdMath.ComputeBedDays(allocatedAt, asOf).Should().Be(1);
    }
}

public class IpdMathFormatAgeTests
{
    [Fact]
    public void DateOfBirth_TwoOrMoreYears_FormatsAsYears()
    {
        var dob = new DateOnly(2020, 1, 1);
        var asOf = new DateOnly(2027, 1, 1);
        IpdMath.FormatAge(dob, null, asOf).Should().Be("7 yr(s)");
    }

    [Fact]
    public void DateOfBirth_UnderTwoYears_FormatsAsMonths()
    {
        var dob = new DateOnly(2026, 6, 1);
        var asOf = new DateOnly(2027, 1, 1);
        IpdMath.FormatAge(dob, null, asOf).Should().Be("7 mo(s)");
    }

    [Fact]
    public void DateOfBirth_BirthdayNotYetReachedThisYear_SubtractsOneYear()
    {
        var dob = new DateOnly(2020, 12, 31);
        var asOf = new DateOnly(2027, 1, 1);
        IpdMath.FormatAge(dob, null, asOf).Should().Be("6 yr(s)");
    }

    [Fact]
    public void NoDateOfBirth_UsesEstimatedAge()
    {
        IpdMath.FormatAge(null, 45, new DateOnly(2027, 1, 1)).Should().Be("~45 yr(s)");
    }

    [Fact]
    public void NeitherDateOfBirthNorEstimatedAge_ReturnsUnknown()
    {
        IpdMath.FormatAge(null, null, new DateOnly(2027, 1, 1)).Should().Be("Unknown");
    }
}
