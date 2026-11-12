using FluentAssertions;
using Prixo.Application.Features.Biomed;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Biomed;

public class SaveEquipmentRequestValidatorTests
{
    private readonly SaveEquipmentRequestValidator _validator = new();

    [Fact]
    public void EmptyLocation_Fails() =>
        _validator.Validate(new SaveEquipmentRequest("EQ-1", "Ventilator", null, null, null, null, null, EquipmentCriticality.High, "", null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SaveEquipmentRequest("EQ-1", "Ventilator", "SN1", "Model X", "Acme", null, null, EquipmentCriticality.High, "ICU", null, null, null, null))
            .IsValid.Should().BeTrue();
}

public class SavePmScheduleRequestValidatorTests
{
    private readonly SavePmScheduleRequestValidator _validator = new();

    [Fact]
    public void ZeroFrequency_Fails() =>
        _validator.Validate(new SavePmScheduleRequest(1, 0, new DateOnly(2026, 8, 1), null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SavePmScheduleRequest(1, 90, new DateOnly(2026, 8, 1), "Check filters")).IsValid.Should().BeTrue();
}

public class RaiseComplaintRequestValidatorTests
{
    private readonly RaiseComplaintRequestValidator _validator = new();

    [Fact]
    public void EmptyDescription_Fails() =>
        _validator.Validate(new RaiseComplaintRequest(1, "")).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RaiseComplaintRequest(1, "Not powering on")).IsValid.Should().BeTrue();
}

public class RecordSpareUsageRequestValidatorTests
{
    private readonly RecordSpareUsageRequestValidator _validator = new();

    [Fact]
    public void ZeroQuantity_Fails() =>
        _validator.Validate(new RecordSpareUsageRequest(1, 0)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RecordSpareUsageRequest(1, 2)).IsValid.Should().BeTrue();
}
