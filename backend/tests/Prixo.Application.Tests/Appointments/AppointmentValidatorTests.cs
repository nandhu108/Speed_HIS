using FluentAssertions;
using Prixo.Application.Features.Appointments;

namespace Prixo.Application.Tests.Appointments;

public class ReplaceDoctorScheduleRequestValidatorTests
{
    private readonly ReplaceDoctorScheduleRequestValidator _validator = new();

    [Fact]
    public void NonOverlappingBlocks_Pass()
    {
        var request = new ReplaceDoctorScheduleRequest(
        [
            new SaveDoctorScheduleBlockRequest(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0), 15),
            new SaveDoctorScheduleBlockRequest(DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(17, 0), 15),
        ]);
        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void OverlappingBlocksOnSameDay_Fail()
    {
        var request = new ReplaceDoctorScheduleRequest(
        [
            new SaveDoctorScheduleBlockRequest(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0), 15),
            new SaveDoctorScheduleBlockRequest(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(14, 0), 15),
        ]);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SameHoursOnDifferentDays_Pass()
    {
        var request = new ReplaceDoctorScheduleRequest(
        [
            new SaveDoctorScheduleBlockRequest(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0), 15),
            new SaveDoctorScheduleBlockRequest(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(12, 0), 15),
        ]);
        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EndBeforeStart_Fails()
    {
        var request = new ReplaceDoctorScheduleRequest(
            [new SaveDoctorScheduleBlockRequest(DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(9, 0), 15)]);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}

public class SaveScheduleOverrideRequestValidatorTests
{
    private readonly SaveScheduleOverrideRequestValidator _validator = new();

    [Fact]
    public void FullDayLeave_WithoutHours_Passes()
    {
        var request = new SaveScheduleOverrideRequest(new DateOnly(2026, 8, 1), false, null, null, null, "On leave");
        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void AvailableOverride_WithoutHours_Fails()
    {
        var request = new SaveScheduleOverrideRequest(new DateOnly(2026, 8, 1), true, null, null, null, null);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AvailableOverride_WithHours_Passes()
    {
        var request = new SaveScheduleOverrideRequest(
            new DateOnly(2026, 8, 1), true, new TimeOnly(10, 0), new TimeOnly(13, 0), 20, "Special clinic");
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}

public class BookAppointmentRequestValidatorTests
{
    private readonly BookAppointmentRequestValidator _validator = new();

    [Fact]
    public void ZeroPatientId_Fails()
    {
        var request = new BookAppointmentRequest(
            0, 1, new DateOnly(2026, 8, 1), new TimeOnly(9, 0), Domain.Enums.AppointmentBookingType.Booked, null);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new BookAppointmentRequest(
            1, 1, new DateOnly(2026, 8, 1), new TimeOnly(9, 0), Domain.Enums.AppointmentBookingType.Booked, null);
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}

public class CancelAppointmentRequestValidatorTests
{
    private readonly CancelAppointmentRequestValidator _validator = new();

    [Fact]
    public void EmptyReason_Fails() =>
        _validator.Validate(new CancelAppointmentRequest("")).IsValid.Should().BeFalse();

    [Fact]
    public void WithReason_Passes() =>
        _validator.Validate(new CancelAppointmentRequest("Patient unavailable")).IsValid.Should().BeTrue();
}

public class RescheduleAppointmentRequestValidatorTests
{
    private readonly RescheduleAppointmentRequestValidator _validator = new();

    [Fact]
    public void EmptyReason_Fails() =>
        _validator.Validate(new RescheduleAppointmentRequest(new DateOnly(2026, 8, 2), new TimeOnly(9, 0), ""))
            .IsValid.Should().BeFalse();

    [Fact]
    public void WithReason_Passes() =>
        _validator.Validate(new RescheduleAppointmentRequest(new DateOnly(2026, 8, 2), new TimeOnly(9, 0), "Requested"))
            .IsValid.Should().BeTrue();
}

public class TransferTokenRequestValidatorTests
{
    private readonly TransferTokenRequestValidator _validator = new();

    [Fact]
    public void ZeroNewDoctorId_Fails() =>
        _validator.Validate(new TransferTokenRequest(0, "Doctor unavailable")).IsValid.Should().BeFalse();

    [Fact]
    public void NullReason_Passes() =>
        _validator.Validate(new TransferTokenRequest(2, null)).IsValid.Should().BeTrue();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new TransferTokenRequest(2, "Doctor unavailable")).IsValid.Should().BeTrue();
}

public class SaveTeleconsultLinkRequestValidatorTests
{
    private readonly SaveTeleconsultLinkRequestValidator _validator = new();

    [Fact]
    public void NullLink_Passes() =>
        _validator.Validate(new SaveTeleconsultLinkRequest(null)).IsValid.Should().BeTrue();

    [Fact]
    public void TooLongLink_Fails() =>
        _validator.Validate(new SaveTeleconsultLinkRequest(new string('a', 501))).IsValid.Should().BeFalse();

    [Fact]
    public void ValidLink_Passes() =>
        _validator.Validate(new SaveTeleconsultLinkRequest("https://meet.example.com/abc-defg-hij")).IsValid.Should().BeTrue();
}
