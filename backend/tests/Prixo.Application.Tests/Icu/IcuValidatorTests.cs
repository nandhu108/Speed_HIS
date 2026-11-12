using FluentAssertions;
using Prixo.Application.Features.Icu;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Icu;

public class AddVentilatorObservationRequestValidatorTests
{
    private readonly AddVentilatorObservationRequestValidator _validator = new();

    [Fact]
    public void FiO2BelowRoomAir_Fails() =>
        _validator.Validate(new AddVentilatorObservationRequest(VentilatorMode.Simv, 15, 5, null, null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new AddVentilatorObservationRequest(VentilatorMode.Simv, 40, 5, 450, 14, 22.5m, 18.0m, null))
            .IsValid.Should().BeTrue();
}

public class StartInfusionRequestValidatorTests
{
    private readonly StartInfusionRequestValidator _validator = new();

    [Fact]
    public void NeitherDrugNorNameOverride_Fails() =>
        _validator.Validate(new StartInfusionRequest(null, null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void DrugId_Passes() => _validator.Validate(new StartInfusionRequest(1, null, null)).IsValid.Should().BeTrue();

    [Fact]
    public void NameOverride_Passes() => _validator.Validate(new StartInfusionRequest(null, "Normal Saline", null)).IsValid.Should().BeTrue();
}

public class AddInfusionRateRequestValidatorTests
{
    private readonly AddInfusionRateRequestValidator _validator = new();

    [Fact]
    public void ZeroRate_Fails() => _validator.Validate(new AddInfusionRateRequest(0, "ml/hr", null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() => _validator.Validate(new AddInfusionRateRequest(5, "ml/hr", null)).IsValid.Should().BeTrue();
}
