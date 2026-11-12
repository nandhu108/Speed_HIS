using FluentAssertions;
using Prixo.Application.Common.Utilities;

namespace Prixo.Application.Tests.Common;

public class TextSimilarityTests
{
    [Fact]
    public void IdenticalNames_ScoreOne() =>
        TextSimilarity.NameSimilarity("Ravi Kumar", "Ravi Kumar").Should().Be(1);

    [Fact]
    public void CaseAndWhitespace_AreIgnored() =>
        TextSimilarity.NameSimilarity("ravi   kumar", "RAVI KUMAR").Should().Be(1);

    [Fact]
    public void MinorTypo_ScoresHigh() =>
        TextSimilarity.NameSimilarity("Ravi Kumar", "Ravee Kumar").Should().BeGreaterThan(0.7);

    [Fact]
    public void UnrelatedNames_ScoreLow() =>
        TextSimilarity.NameSimilarity("Ravi Kumar", "Priya Sharma").Should().BeLessThan(0.4);

    [Fact]
    public void EmptyStrings_DoNotThrow() =>
        TextSimilarity.NameSimilarity("", "").Should().Be(1);

    [Theory]
    [InlineData("", "abc", 3)]
    [InlineData("abc", "", 3)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("abc", "abc", 0)]
    public void LevenshteinDistance_MatchesKnownValues(string a, string b, int expected) =>
        TextSimilarity.LevenshteinDistance(a, b).Should().Be(expected);
}
