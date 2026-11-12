using FluentAssertions;
using Prixo.Application.Features.Emr;

namespace Prixo.Application.Tests.Emr;

public class EmrMathTryParseTrendValueTests
{
    [Fact]
    public void IntegerValue_Parses() => EmrMath.TryParseTrendValue("120").Should().Be(120m);

    [Fact]
    public void DecimalValue_Parses() => EmrMath.TryParseTrendValue("98.6").Should().Be(98.6m);

    [Fact]
    public void QualitativeValue_ReturnsNull() => EmrMath.TryParseTrendValue("Positive").Should().BeNull();

    [Fact]
    public void EmptyValue_ReturnsNull() => EmrMath.TryParseTrendValue("").Should().BeNull();

    [Fact]
    public void NegativeValue_Parses() => EmrMath.TryParseTrendValue("-2.5").Should().Be(-2.5m);
}

public class EmrMathMatchesAllergenTests
{
    [Fact]
    public void ExactMatch_CaseInsensitive_Matches() =>
        EmrMath.MatchesAllergen("paracetamol", "Paracetamol").Should().BeTrue();

    [Fact]
    public void CandidateContainsAllergen_Matches() =>
        EmrMath.MatchesAllergen("Paracetamol 500mg", "Paracetamol").Should().BeTrue();

    [Fact]
    public void AllergenContainsCandidate_Matches() =>
        EmrMath.MatchesAllergen("Penicillin", "Penicillin G Injection").Should().BeTrue();

    [Fact]
    public void UnrelatedNames_DoNotMatch() =>
        EmrMath.MatchesAllergen("Paracetamol", "Penicillin").Should().BeFalse();

    [Fact]
    public void EmptyCandidate_DoesNotMatch() =>
        EmrMath.MatchesAllergen("", "Penicillin").Should().BeFalse();

    [Fact]
    public void EmptyAllergen_DoesNotMatch() =>
        EmrMath.MatchesAllergen("Paracetamol", "   ").Should().BeFalse();

    [Fact]
    public void WhitespacePadding_IsTrimmedBeforeMatching() =>
        EmrMath.MatchesAllergen("  Paracetamol  ", "paracetamol").Should().BeTrue();
}
