using FluentAssertions;
using Prixo.Application.Features.Er;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Er;

public class RegisterEmergencyVisitRequestValidatorTests
{
    private readonly RegisterEmergencyVisitRequestValidator _validator = new();

    [Fact]
    public void ZeroPatientId_Fails() =>
        _validator.Validate(new RegisterEmergencyVisitRequest(0, ArrivalMode.WalkIn, false)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RegisterEmergencyVisitRequest(1, ArrivalMode.Ambulance, true)).IsValid.Should().BeTrue();
}

public class RecordTriageRequestValidatorTests
{
    private readonly RecordTriageRequestValidator _validator = new();

    [Fact]
    public void EmptyChiefComplaint_Fails() =>
        _validator.Validate(new RecordTriageRequest(EsiLevel.Urgent, "", null, null, null, null, null, null, null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RecordTriageRequest(EsiLevel.Urgent, "Chest pain", 98.6m, 90, 18, 120, 80, 97, null, null)).IsValid.Should().BeTrue();
}

public class SaveAssessmentRequestValidatorTests
{
    private readonly SaveAssessmentRequestValidator _validator = new();

    [Fact]
    public void TransferredOut_WithoutFacility_Fails() =>
        _validator.Validate(new SaveAssessmentRequest(1, null, null, null, null, DispositionType.TransferredOut, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void TransferredOut_WithFacility_Passes() =>
        _validator.Validate(new SaveAssessmentRequest(1, null, null, null, null, DispositionType.TransferredOut, null, "City Hospital", "Summary"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void Discharged_WithoutFacility_Passes() =>
        _validator.Validate(new SaveAssessmentRequest(1, null, null, null, null, DispositionType.Discharged, null, null, null))
            .IsValid.Should().BeTrue();
}
