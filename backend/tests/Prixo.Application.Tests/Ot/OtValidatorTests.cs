using FluentAssertions;
using Prixo.Application.Features.Ot;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Ot;

public class CreateBookingRequestValidatorTests
{
    private readonly CreateBookingRequestValidator _validator = new();
    private static readonly DateTimeOffset Start = new(2026, 7, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EndBeforeStart_Fails() =>
        _validator.Validate(new CreateBookingRequest(1, 1, "Appendectomy", 1, null, Start, Start.AddHours(-1), null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void EmptyProcedureName_Fails() =>
        _validator.Validate(new CreateBookingRequest(1, 1, "", 1, null, Start, Start.AddHours(1), null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new CreateBookingRequest(1, 1, "Appendectomy", 1, 2, Start, Start.AddHours(1), null))
            .IsValid.Should().BeTrue();
}

public class UpdateBookingPatientRequestValidatorTests
{
    private readonly UpdateBookingPatientRequestValidator _validator = new();

    [Fact]
    public void ZeroAdmissionId_Fails() => _validator.Validate(new UpdateBookingPatientRequest(0)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidAdmissionId_Passes() => _validator.Validate(new UpdateBookingPatientRequest(5)).IsValid.Should().BeTrue();
}

public class CancelBookingRequestValidatorTests
{
    private readonly CancelBookingRequestValidator _validator = new();

    [Fact]
    public void EmptyReason_Fails() => _validator.Validate(new CancelBookingRequest("")).IsValid.Should().BeFalse();

    [Fact]
    public void ValidReason_Passes() => _validator.Validate(new CancelBookingRequest("Patient unfit for surgery")).IsValid.Should().BeTrue();
}

public class AddTeamMemberRequestValidatorTests
{
    private readonly AddTeamMemberRequestValidator _validator = new();

    [Fact]
    public void NeitherDoctorNorStaffName_Fails() =>
        _validator.Validate(new AddTeamMemberRequest(SurgeryTeamRole.ScrubNurse, null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void DoctorId_Passes() =>
        _validator.Validate(new AddTeamMemberRequest(SurgeryTeamRole.AssistantSurgeon, 1, null)).IsValid.Should().BeTrue();

    [Fact]
    public void StaffName_Passes() =>
        _validator.Validate(new AddTeamMemberRequest(SurgeryTeamRole.ScrubNurse, null, "Nurse Joy")).IsValid.Should().BeTrue();
}

public class RecordImplantUsageRequestValidatorTests
{
    private readonly RecordImplantUsageRequestValidator _validator = new();

    [Fact]
    public void ZeroQuantity_Fails() => _validator.Validate(new RecordImplantUsageRequest(1, "SN123", 0)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() => _validator.Validate(new RecordImplantUsageRequest(1, "SN123", 1)).IsValid.Should().BeTrue();
}
