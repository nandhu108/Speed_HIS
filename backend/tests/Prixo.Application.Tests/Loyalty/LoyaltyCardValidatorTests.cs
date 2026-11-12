using FluentAssertions;
using Prixo.Application.Features.Loyalty;

namespace Prixo.Application.Tests.Loyalty;

public class RegisterLoyaltyCardRequestValidatorTests
{
    private readonly RegisterLoyaltyCardRequestValidator _validator = new();

    [Fact]
    public void ZeroPatientId_Fails() =>
        _validator.Validate(new RegisterLoyaltyCardRequest(0, 1, 12)).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroDurationMonths_Fails() =>
        _validator.Validate(new RegisterLoyaltyCardRequest(1, 1, 0)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RegisterLoyaltyCardRequest(1, 1, 12)).IsValid.Should().BeTrue();
}

public class RenewLoyaltyCardRequestValidatorTests
{
    private readonly RenewLoyaltyCardRequestValidator _validator = new();

    [Fact]
    public void ZeroDurationMonths_Fails() =>
        _validator.Validate(new RenewLoyaltyCardRequest(null, 0)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RenewLoyaltyCardRequest(2, 12)).IsValid.Should().BeTrue();
}

public class AddDependentRequestValidatorTests
{
    private readonly AddDependentRequestValidator _validator = new();

    [Fact]
    public void ZeroPatientId_Fails() =>
        _validator.Validate(new AddDependentRequest(0, 1)).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroRelationTagId_Fails() =>
        _validator.Validate(new AddDependentRequest(1, 0)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new AddDependentRequest(1, 1)).IsValid.Should().BeTrue();
}

public class DeactivateLoyaltyCardRequestValidatorTests
{
    private readonly DeactivateLoyaltyCardRequestValidator _validator = new();

    [Fact]
    public void EmptyReason_Fails() =>
        _validator.Validate(new DeactivateLoyaltyCardRequest("")).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new DeactivateLoyaltyCardRequest("Lost card, member request")).IsValid.Should().BeTrue();
}
