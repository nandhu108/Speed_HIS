using FluentAssertions;
using Prixo.Application.Features.Masters.PatientCategories;

namespace Prixo.Application.Tests.Masters;

public class SavePatientCategoryRequestValidatorTests
{
    private readonly SavePatientCategoryRequestValidator _validator = new();

    [Fact]
    public void EmptyCode_Fails() =>
        _validator.Validate(new SavePatientCategoryRequest("", "Corporate", null, "#3B82F6")).IsValid.Should().BeFalse();

    [Fact]
    public void LowercaseCode_Fails() =>
        _validator.Validate(new SavePatientCategoryRequest("corp", "Corporate", null, "#3B82F6")).IsValid.Should().BeFalse();

    [Fact]
    public void InvalidColorHex_Fails() =>
        _validator.Validate(new SavePatientCategoryRequest("CORP", "Corporate", null, "blue")).IsValid.Should().BeFalse();

    [Fact]
    public void NullColorHex_Passes() =>
        _validator.Validate(new SavePatientCategoryRequest("CORP", "Corporate", null, null)).IsValid.Should().BeTrue();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SavePatientCategoryRequest("CORP", "Corporate", "Corporate-billed patients", "#3B82F6")).IsValid.Should().BeTrue();
}
