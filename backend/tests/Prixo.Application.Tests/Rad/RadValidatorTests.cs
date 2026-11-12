using FluentAssertions;
using Prixo.Application.Features.Rad;

namespace Prixo.Application.Tests.Rad;

public class SaveRadiologyTestRequestValidatorTests
{
    private readonly SaveRadiologyTestRequestValidator _validator = new();

    private static SaveRadiologyTestRequest Valid() => new("XRC", "X-Ray Chest", 1, "Chest", false, null, null, 4);

    [Fact]
    public void ValidRequest_Passes() => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void EmptyCode_Fails() => _validator.Validate(Valid() with { Code = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroModalityId_Fails() => _validator.Validate(Valid() with { ModalityId = 0 }).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroTatHours_Fails() => _validator.Validate(Valid() with { TatHours = 0 }).IsValid.Should().BeFalse();
}

public class CreateRadOrderRequestValidatorTests
{
    private readonly CreateRadOrderRequestValidator _validator = new();

    [Fact]
    public void EmptyTestList_Fails() =>
        _validator.Validate(new CreateRadOrderRequest(1, null, null, null, null, null, Domain.Enums.ClinicalOrderPriority.Routine, null, []))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new CreateRadOrderRequest(1, null, null, null, null, null, Domain.Enums.ClinicalOrderPriority.Routine, null, [1]))
            .IsValid.Should().BeTrue();
}

public class EnterReportRequestValidatorTests
{
    private readonly EnterReportRequestValidator _validator = new();

    [Fact]
    public void EmptyFindings_Fails() =>
        _validator.Validate(new EnterReportRequest(null, "", "Normal study.")).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new EnterReportRequest(null, "No acute abnormality.", "Normal study.")).IsValid.Should().BeTrue();
}

public class CaptureContrastConsentRequestValidatorTests
{
    private readonly CaptureContrastConsentRequestValidator _validator = new();

    [Fact]
    public void EmptySignedByName_Fails() =>
        _validator.Validate(new CaptureContrastConsentRequest("", null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new CaptureContrastConsentRequest("Self", null)).IsValid.Should().BeTrue();
}
