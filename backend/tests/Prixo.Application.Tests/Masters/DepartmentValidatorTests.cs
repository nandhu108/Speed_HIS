using FluentAssertions;
using Prixo.Application.Features.Masters.Departments;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Masters;

public class SaveDepartmentRequestValidatorTests
{
    private readonly SaveDepartmentRequestValidator _validator = new();

    [Theory]
    [InlineData(DepartmentType.Clinical)]
    [InlineData(DepartmentType.NonClinical)]
    public void ValidTypes_Pass(DepartmentType type) =>
        _validator.Validate(new SaveDepartmentRequest("DEP001", "General Medicine", null, type))
            .IsValid.Should().BeTrue();

    [Fact]
    public void UndefinedType_Fails() =>
        _validator.Validate(new SaveDepartmentRequest("DEP001", "General Medicine", null, (DepartmentType)0))
            .IsValid.Should().BeFalse();

    [Fact]
    public void LowercaseCode_Fails() =>
        _validator.Validate(new SaveDepartmentRequest("dep001", "General Medicine", null))
            .IsValid.Should().BeFalse();
}
