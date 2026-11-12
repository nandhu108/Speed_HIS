using FluentAssertions;
using Prixo.Application.Features.Portal;

namespace Prixo.Application.Tests.Portal;

public class RegisterPortalAccountRequestValidatorTests
{
    private readonly RegisterPortalAccountRequestValidator _validator = new();

    [Fact]
    public void ShortPassword_Fails() =>
        _validator.Validate(new RegisterPortalAccountRequest("UHID-1", "9999999999", new DateOnly(1990, 1, 1), "short")).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RegisterPortalAccountRequest("UHID-1", "9999999999", new DateOnly(1990, 1, 1), "longenoughpw")).IsValid.Should().BeTrue();
}

public class PortalLoginRequestValidatorTests
{
    private readonly PortalLoginRequestValidator _validator = new();

    [Fact]
    public void EmptyPassword_Fails() =>
        _validator.Validate(new PortalLoginRequest("UHID-1", "")).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new PortalLoginRequest("UHID-1", "password123")).IsValid.Should().BeTrue();
}

public class PortalChangePasswordRequestValidatorTests
{
    private readonly PortalChangePasswordRequestValidator _validator = new();

    [Fact]
    public void ShortNewPassword_Fails() =>
        _validator.Validate(new PortalChangePasswordRequest("current123", "short")).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new PortalChangePasswordRequest("current123", "newlongenough")).IsValid.Should().BeTrue();
}
