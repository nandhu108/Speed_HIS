using FluentAssertions;
using Prixo.Application.Features.Emr;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Emr;

public class AddMedicalHistoryRequestValidatorTests
{
    private readonly AddMedicalHistoryRequestValidator _validator = new();

    [Fact]
    public void EmptyCondition_Fails() =>
        _validator.Validate(new AddMedicalHistoryRequest("", null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new AddMedicalHistoryRequest("Diabetes Mellitus", new DateOnly(2020, 1, 1), null)).IsValid.Should().BeTrue();
}

public class AddAllergyRequestValidatorTests
{
    private readonly AddAllergyRequestValidator _validator = new();

    [Fact]
    public void EmptyAllergenName_Fails() =>
        _validator.Validate(new AddAllergyRequest(AllergenType.Drug, "", null, AllergySeverity.Moderate)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new AddAllergyRequest(AllergenType.Drug, "Penicillin", "Rash", AllergySeverity.Severe)).IsValid.Should().BeTrue();
}

public class AddProblemRequestValidatorTests
{
    private readonly AddProblemRequestValidator _validator = new();

    [Fact]
    public void EmptyDiagnosisText_Fails() =>
        _validator.Validate(new AddProblemRequest(null, "", new DateOnly(2027, 1, 1), null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new AddProblemRequest(null, "Type 2 Diabetes", new DateOnly(2027, 1, 1), null)).IsValid.Should().BeTrue();
}

public class AddClinicalNoteRequestValidatorTests
{
    private readonly AddClinicalNoteRequestValidator _validator = new();

    [Fact]
    public void EmptyContent_Fails() =>
        _validator.Validate(new AddClinicalNoteRequest("General", null, "", null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new AddClinicalNoteRequest("General", "Follow-up", "Patient doing well.", null, null)).IsValid.Should().BeTrue();
}
