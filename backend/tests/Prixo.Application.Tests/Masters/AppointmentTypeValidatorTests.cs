using FluentAssertions;
using Prixo.Application.Features.Masters.AppointmentTypes;

namespace Prixo.Application.Tests.Masters;

public class SaveAppointmentTypeRequestValidatorTests
{
    private readonly SaveAppointmentTypeRequestValidator _validator = new();

    [Fact]
    public void EmptyCode_Fails() =>
        _validator.Validate(new SaveAppointmentTypeRequest("", "New Consultation", null, 15)).IsValid.Should().BeFalse();

    [Fact]
    public void LowercaseCode_Fails() =>
        _validator.Validate(new SaveAppointmentTypeRequest("new", "New Consultation", null, 15)).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroSlotMinutes_Fails() =>
        _validator.Validate(new SaveAppointmentTypeRequest("NEW", "New Consultation", null, 0)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SaveAppointmentTypeRequest("NEW", "New Consultation", "First visit", 15)).IsValid.Should().BeTrue();
}
