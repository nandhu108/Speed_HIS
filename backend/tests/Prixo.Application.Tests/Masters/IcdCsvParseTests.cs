using System.Text;
using FluentAssertions;
using Prixo.Application.Features.Masters.Icd;

namespace Prixo.Application.Tests.Masters;

public class IcdCsvParseTests
{
    private static Stream ToStream(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void ParsesRows_WithHeader()
    {
        var csv = "code,description,category\nA00,Cholera,Infectious\nI10,Essential hypertension,Circulatory";
        var rows = IcdService.ParseCsv(ToStream(csv), out var errors);

        errors.Should().BeEmpty();
        rows.Should().HaveCount(2);
        rows[0].Should().Be(("A00", "Cholera", "Infectious"));
        rows[1].Should().Be(("I10", "Essential hypertension", "Circulatory"));
    }

    [Fact]
    public void ParsesRows_WithoutHeaderOrCategory()
    {
        var csv = "A00,Cholera\nB01,Varicella";
        var rows = IcdService.ParseCsv(ToStream(csv), out var errors);

        errors.Should().BeEmpty();
        rows.Should().HaveCount(2);
        rows[0].Should().Be(("A00", "Cholera", null));
    }

    [Fact]
    public void HandlesQuotedFields_WithCommasAndEscapedQuotes()
    {
        var csv = "A00,\"Cholera, unspecified\",\"Chapter \"\"I\"\"\"";
        var rows = IcdService.ParseCsv(ToStream(csv), out var errors);

        errors.Should().BeEmpty();
        rows.Single().Should().Be(("A00", "Cholera, unspecified", "Chapter \"I\""));
    }

    [Fact]
    public void UppercasesCodes_AndSkipsDuplicatesInFile()
    {
        var csv = "a00,Cholera\nA00,Cholera again";
        var rows = IcdService.ParseCsv(ToStream(csv), out var errors);

        rows.Single().Item1.Should().Be("A00");
        errors.Single().Should().Contain("duplicate");
    }

    [Fact]
    public void ReportsRows_MissingDescription()
    {
        var csv = "A00,Cholera\nB01,";
        var rows = IcdService.ParseCsv(ToStream(csv), out var errors);

        rows.Should().HaveCount(1);
        errors.Single().Should().Contain("Line 2");
    }

    [Fact]
    public void SkipsBlankLines()
    {
        var csv = "A00,Cholera\n\n\nB01,Varicella\n";
        var rows = IcdService.ParseCsv(ToStream(csv), out var errors);

        errors.Should().BeEmpty();
        rows.Should().HaveCount(2);
    }

    [Fact]
    public void RejectsCodes_LongerThan20Chars()
    {
        var csv = $"{new string('A', 21)},Too long";
        var rows = IcdService.ParseCsv(ToStream(csv), out var errors);

        rows.Should().BeEmpty();
        errors.Single().Should().Contain("exceeds 20");
    }
}
