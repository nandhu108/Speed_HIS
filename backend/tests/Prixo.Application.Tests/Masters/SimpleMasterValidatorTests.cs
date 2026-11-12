using FluentAssertions;
using Prixo.Application.Features.Masters.Common;

namespace Prixo.Application.Tests.Masters;

public class SimpleMasterValidatorTests
{
    private readonly SaveSimpleMasterRequestValidator _validator = new();

    [Theory]
    [InlineData("OPD")]
    [InlineData("GEN-1")]
    [InlineData("ITEM_CAT")]
    [InlineData("A1")]
    public void ValidCodes_Pass(string code)
    {
        var result = _validator.Validate(new SaveSimpleMasterRequest(code, "Name", null));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("opd")]           // lowercase
    [InlineData("OPD X")]         // space
    [InlineData("OPD.X")]         // dot
    public void InvalidCodes_Fail(string code)
    {
        var result = _validator.Validate(new SaveSimpleMasterRequest(code, "Name", null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CodeOver30Chars_Fails()
    {
        var result = _validator.Validate(new SaveSimpleMasterRequest(new string('A', 31), "Name", null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyName_Fails()
    {
        var result = _validator.Validate(new SaveSimpleMasterRequest("CODE", "", null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DescriptionOver500Chars_Fails()
    {
        var result = _validator.Validate(new SaveSimpleMasterRequest("CODE", "Name", new string('x', 501)));
        result.IsValid.Should().BeFalse();
    }
}
