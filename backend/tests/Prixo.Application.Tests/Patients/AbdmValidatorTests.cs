using FluentAssertions;
using Prixo.Application.Features.Patients.Abdm;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Patients;

public class InitiateAbhaVerificationRequestValidatorTests
{
    private readonly InitiateAbhaVerificationRequestValidator _validator = new();

    [Fact]
    public void EmptyIdentifier_Fails() =>
        _validator.Validate(new InitiateAbhaVerificationRequest(AbdmVerificationMethod.MobileOtp, ""))
            .IsValid.Should().BeFalse();

    [Fact]
    public void MobileOtp_WithIdentifier_Passes() =>
        _validator.Validate(new InitiateAbhaVerificationRequest(AbdmVerificationMethod.MobileOtp, "9999999999"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void AadhaarOtp_WithIdentifier_Passes() =>
        _validator.Validate(new InitiateAbhaVerificationRequest(AbdmVerificationMethod.AadhaarOtp, "123412341234"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void UndefinedMethod_Fails() =>
        _validator.Validate(new InitiateAbhaVerificationRequest((AbdmVerificationMethod)99, "9999999999"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void OverlongIdentifier_Fails() =>
        _validator.Validate(new InitiateAbhaVerificationRequest(AbdmVerificationMethod.AbhaOtp, new string('9', 51)))
            .IsValid.Should().BeFalse();
}

public class CompleteAbhaVerificationRequestValidatorTests
{
    private readonly CompleteAbhaVerificationRequestValidator _validator = new();

    [Fact]
    public void ValidOtp_Passes() =>
        _validator.Validate(new CompleteAbhaVerificationRequest(5, "123456")).IsValid.Should().BeTrue();

    [Fact]
    public void NonNumericOtp_Fails() =>
        _validator.Validate(new CompleteAbhaVerificationRequest(5, "abc")).IsValid.Should().BeFalse();

    [Fact]
    public void EmptyOtp_Fails() =>
        _validator.Validate(new CompleteAbhaVerificationRequest(5, "")).IsValid.Should().BeFalse();

    [Fact]
    public void NonPositiveSessionId_Fails() =>
        _validator.Validate(new CompleteAbhaVerificationRequest(0, "123456")).IsValid.Should().BeFalse();
}

public class UnlinkAbhaRequestValidatorTests
{
    private readonly UnlinkAbhaRequestValidator _validator = new();

    [Fact]
    public void EmptyReason_Fails() =>
        _validator.Validate(new UnlinkAbhaRequest("")).IsValid.Should().BeFalse();

    [Fact]
    public void WithReason_Passes() =>
        _validator.Validate(new UnlinkAbhaRequest("Wrong ABHA linked")).IsValid.Should().BeTrue();
}

public class ScanAndShareQrRequestValidatorTests
{
    private readonly ScanAndShareQrRequestValidator _validator = new();

    [Fact]
    public void EmptyPayload_Fails() =>
        _validator.Validate(new ScanAndShareQrRequest("")).IsValid.Should().BeFalse();

    [Fact]
    public void ValidPayload_Passes() =>
        _validator.Validate(new ScanAndShareQrRequest("{\"hidn\":\"91-1234-5678-9012\",\"name\":\"Ramesh\"}"))
            .IsValid.Should().BeTrue();
}

public class InitiateConsentRequestDtoValidatorTests
{
    private readonly InitiateConsentRequestDtoValidator _validator = new();

    [Fact]
    public void EmptyHiTypes_Fails() =>
        _validator.Validate(new InitiateConsentRequestDto(
            1, "CAET", [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidConsentRequest_Passes() =>
        _validator.Validate(new InitiateConsentRequestDto(
            1, "CAET", [AbdmHiType.OpConsultation, AbdmHiType.Prescription],
            DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7)))
            .IsValid.Should().BeTrue();
}

public class UpdateAbdmAdminConfigRequestValidatorTests
{
    private readonly UpdateAbdmAdminConfigRequestValidator _validator = new();

    [Fact]
    public void ValidAdminConfig_Passes() =>
        _validator.Validate(new UpdateAbdmAdminConfigRequest(
            true, true, true, true, "https://dev.abdm.gov.in/gateway", "SBX_PRIXO", "secret", "HIP-001", "HIU-001"))
            .IsValid.Should().BeTrue();
}
