using FluentAssertions;
using Prixo.Application.Features.Lab;

namespace Prixo.Application.Tests.Lab;

public class CreateLabOrderItemRequestValidatorTests
{
    private readonly CreateLabOrderItemRequestValidator _validator = new();

    [Fact]
    public void BothNull_Fails() =>
        _validator.Validate(new CreateLabOrderItemRequest(null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void BothSet_Fails() =>
        _validator.Validate(new CreateLabOrderItemRequest(1, 1)).IsValid.Should().BeFalse();

    [Fact]
    public void OnlyLabTestId_Passes() =>
        _validator.Validate(new CreateLabOrderItemRequest(1, null)).IsValid.Should().BeTrue();

    [Fact]
    public void OnlyTestProfileId_Passes() =>
        _validator.Validate(new CreateLabOrderItemRequest(null, 1)).IsValid.Should().BeTrue();
}

public class CreateLabOrderRequestValidatorTests
{
    private readonly CreateLabOrderRequestValidator _validator = new();

    [Fact]
    public void NoItems_Fails() =>
        _validator.Validate(new CreateLabOrderRequest(1, null, null, null, null, Domain.Enums.ClinicalOrderPriority.Routine, null, []))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ZeroPatientId_Fails() =>
        _validator.Validate(new CreateLabOrderRequest(0, null, null, null, null, Domain.Enums.ClinicalOrderPriority.Routine, null,
            [new CreateLabOrderItemRequest(1, null)])).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new CreateLabOrderRequest(1, null, null, null, null, Domain.Enums.ClinicalOrderPriority.Routine, null,
            [new CreateLabOrderItemRequest(1, null)])).IsValid.Should().BeTrue();
}

public class SaveReferenceRangeRequestValidatorTests
{
    private readonly SaveReferenceRangeRequestValidator _validator = new();

    [Fact]
    public void MinAgeGreaterThanMaxAge_Fails() =>
        _validator.Validate(new SaveReferenceRangeRequest(null, 60, 18, null, null, null, null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void NormalLowGreaterThanNormalHigh_Fails() =>
        _validator.Validate(new SaveReferenceRangeRequest(null, null, null, 20, 10, null, null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRange_Passes() =>
        _validator.Validate(new SaveReferenceRangeRequest(null, 0, 120, 12, 16, 5, 20, null)).IsValid.Should().BeTrue();

    [Fact]
    public void AllNull_Passes() =>
        _validator.Validate(new SaveReferenceRangeRequest(null, null, null, null, null, null, null, null)).IsValid.Should().BeTrue();
}

public class SaveLabTestRequestValidatorTests
{
    private readonly SaveLabTestRequestValidator _validator = new();

    private static SaveLabTestParameterRequest ValidParameter() =>
        new("HB", "Hemoglobin", "g/dL", Domain.Enums.ParameterValueType.Numeric, null, 1, []);

    [Fact]
    public void NoParameters_Fails() =>
        _validator.Validate(new SaveLabTestRequest("HB", "Hemoglobin", null, 1, null, null, 4, false, [])).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroSampleTypeId_Fails() =>
        _validator.Validate(new SaveLabTestRequest("HB", "Hemoglobin", null, 0, null, null, 4, false, [ValidParameter()]))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ZeroTatHours_Fails() =>
        _validator.Validate(new SaveLabTestRequest("HB", "Hemoglobin", null, 1, null, null, 0, false, [ValidParameter()]))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SaveLabTestRequest("HB", "Hemoglobin", null, 1, null, null, 4, false, [ValidParameter()]))
            .IsValid.Should().BeTrue();
}

public class EnterResultRequestValidatorTests
{
    private readonly EnterResultRequestValidator _validator = new();

    [Fact]
    public void NoValues_Fails() =>
        _validator.Validate(new EnterResultRequest([], null)).IsValid.Should().BeFalse();

    [Fact]
    public void EmptyValueString_Fails() =>
        _validator.Validate(new EnterResultRequest([new EnterResultValueRequest(1, "")], null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new EnterResultRequest([new EnterResultValueRequest(1, "14.2")], "Looks fine")).IsValid.Should().BeTrue();
}

public class CollectSampleRequestValidatorTests
{
    private readonly CollectSampleRequestValidator _validator = new();

    [Fact]
    public void NoItemIds_Fails() =>
        _validator.Validate(new CollectSampleRequest(1, 1, [])).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new CollectSampleRequest(1, 1, [1, 2])).IsValid.Should().BeTrue();
}
