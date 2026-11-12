using FluentAssertions;
using Prixo.Application.Features.Nursing;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Nursing;

public class AddNursingNoteRequestValidatorTests
{
    private readonly AddNursingNoteRequestValidator _validator = new();

    [Fact]
    public void EmptyNotes_Fails() =>
        _validator.Validate(new AddNursingNoteRequest(NursingShift.Morning, "")).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new AddNursingNoteRequest(NursingShift.Night, "Patient resting comfortably.")).IsValid.Should().BeTrue();
}

public class AddRiskAssessmentRequestValidatorTests
{
    private readonly AddRiskAssessmentRequestValidator _validator = new();

    [Fact]
    public void EmptyItemScores_Fails() =>
        _validator.Validate(new AddRiskAssessmentRequest(RiskAssessmentType.FallRisk, [])).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new AddRiskAssessmentRequest(RiskAssessmentType.FallRisk, new() { ["history"] = 25 })).IsValid.Should().BeTrue();
}

public class AddCarePlanRequestValidatorTests
{
    private readonly AddCarePlanRequestValidator _validator = new();

    [Fact]
    public void EmptyProblemStatement_Fails() =>
        _validator.Validate(new AddCarePlanRequest("", "Reduce pain", "Administer analgesic")).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new AddCarePlanRequest("Acute pain", "Reduce pain to <=3/10", "Administer analgesic q6h")).IsValid.Should().BeTrue();
}

public class MarActionRequestValidatorTests
{
    private readonly MarActionRequestValidator _validator = new();

    [Fact]
    public void Skipped_WithoutReason_Fails() =>
        _validator.Validate(new MarActionRequest(MarStatus.Skipped, null, null, null, null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void Skipped_WithReason_Passes() =>
        _validator.Validate(new MarActionRequest(MarStatus.Skipped, "Patient asleep", null, null, null, null)).IsValid.Should().BeTrue();

    [Fact]
    public void Administered_WithoutReason_Passes() =>
        _validator.Validate(new MarActionRequest(MarStatus.Administered, null, null, 2, null, null)).IsValid.Should().BeTrue();

    [Fact]
    public void Skipped_WithBatch_Fails() =>
        _validator.Validate(new MarActionRequest(MarStatus.Skipped, "Patient asleep", null, null, 5, 1)).IsValid.Should().BeFalse();

    [Fact]
    public void Administered_WithBatchButNoQuantity_Fails() =>
        _validator.Validate(new MarActionRequest(MarStatus.Administered, null, null, null, 5, null)).IsValid.Should().BeFalse();

    [Fact]
    public void Administered_WithBatchAndQuantity_Passes() =>
        _validator.Validate(new MarActionRequest(MarStatus.Administered, null, null, null, 5, 1)).IsValid.Should().BeTrue();
}

public class ReportIncidentRequestValidatorTests
{
    private readonly ReportIncidentRequestValidator _validator = new();

    private static ReportIncidentRequest Valid() => new(
        DateTimeOffset.UtcNow, "Fall", "Ward A", IncidentSeverity.Moderate, "Patient fell while ambulating.", null, null, null);

    [Fact]
    public void ValidRequest_Passes() => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void EmptyDescription_Fails() => _validator.Validate(Valid() with { Description = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void EmptyLocation_Fails() => _validator.Validate(Valid() with { Location = "" }).IsValid.Should().BeFalse();
}

public class RecordIncidentRcaRequestValidatorTests
{
    private readonly RecordIncidentRcaRequestValidator _validator = new();

    [Fact]
    public void EmptyRootCauseAnalysis_Fails() =>
        _validator.Validate(new RecordIncidentRcaRequest("", true)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RecordIncidentRcaRequest("Missed a hand-off step per the checklist.", true)).IsValid.Should().BeTrue();
}

public class CreateShiftHandoverRequestValidatorTests
{
    private readonly CreateShiftHandoverRequestValidator _validator = new();

    [Fact]
    public void ZeroWardId_Fails() =>
        _validator.Validate(new CreateShiftHandoverRequest(0, new DateOnly(2027, 1, 1), NursingShift.Morning, null, null, [])).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_WithNoPatientNotes_Passes() =>
        _validator.Validate(new CreateShiftHandoverRequest(1, new DateOnly(2027, 1, 1), NursingShift.Morning, null, null, [])).IsValid.Should().BeTrue();

    [Fact]
    public void PatientNoteWithEmptySituation_Fails()
    {
        var request = new CreateShiftHandoverRequest(1, new DateOnly(2027, 1, 1), NursingShift.Morning, null, null,
            [new PatientHandoverNoteRequest(1, "", "Background", "Assessment", "Recommendation")]);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}
