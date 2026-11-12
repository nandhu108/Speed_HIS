using FluentAssertions;
using Prixo.Application.Features.Quality;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Quality;

public class SaveIndicatorRequestValidatorTests
{
    private readonly SaveIndicatorRequestValidator _validator = new();

    [Fact]
    public void LowercaseCode_Fails() =>
        _validator.Validate(new SaveIndicatorRequest("lab_tat", "Lab TAT", null, null, "Tests with result", "Total tests", "hours", null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SaveIndicatorRequest("LAB_TAT", "Lab TAT", null, "Clinical", "Sum of TAT hours", "Test count", "hours", 24, QualityAutoComputeSource.LabTat))
            .IsValid.Should().BeTrue();
}

public class RecordIndicatorEntryRequestValidatorTests
{
    private readonly RecordIndicatorEntryRequestValidator _validator = new();

    [Fact]
    public void ZeroDenominator_Fails() =>
        _validator.Validate(new RecordIndicatorEntryRequest(new DateOnly(2026, 7, 1), 5, 0, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RecordIndicatorEntryRequest(new DateOnly(2026, 7, 1), 5, 50, "On track")).IsValid.Should().BeTrue();
}

public class SaveAuditTemplateRequestValidatorTests
{
    private readonly SaveAuditTemplateRequestValidator _validator = new();

    [Fact]
    public void NoItems_Fails() =>
        _validator.Validate(new SaveAuditTemplateRequest("HAND_HYGIENE", "Hand Hygiene Audit", null, [])).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SaveAuditTemplateRequest("HAND_HYGIENE", "Hand Hygiene Audit", "Infection Control", [
            new SaveAuditTemplateItemRequest(1, "Hand hygiene performed before patient contact?", 5),
        ])).IsValid.Should().BeTrue();
}

public class ScoreAuditRequestValidatorTests
{
    private readonly ScoreAuditRequestValidator _validator = new();

    [Fact]
    public void NoResponses_Fails() =>
        _validator.Validate(new ScoreAuditRequest([])).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new ScoreAuditRequest([new RecordAuditResponseRequest(1, 4, null)])).IsValid.Should().BeTrue();
}

public class RaiseNonConformanceRequestValidatorTests
{
    private readonly RaiseNonConformanceRequestValidator _validator = new();

    [Fact]
    public void EmptyTitle_Fails() =>
        _validator.Validate(new RaiseNonConformanceRequest(null, null, "", "Missing signage", NonConformanceSeverity.Minor))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RaiseNonConformanceRequest(null, null, "Missing fire exit signage", "Corridor B lacks signage", NonConformanceSeverity.Major))
            .IsValid.Should().BeTrue();
}

public class SaveRiskRequestValidatorTests
{
    private readonly SaveRiskRequestValidator _validator = new();

    [Fact]
    public void LikelihoodOutOfRange_Fails() =>
        _validator.Validate(new SaveRiskRequest("Needlestick injury", null, "Sharps handling risk", 6, 3, null, 1, new DateOnly(2026, 7, 1), null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SaveRiskRequest("Needlestick injury", "Clinical", "Sharps handling risk", 3, 4, "PPE training", 1, new DateOnly(2026, 7, 1), null))
            .IsValid.Should().BeTrue();
}

public class RaiseCapaRequestValidatorTests
{
    private readonly RaiseCapaRequestValidator _validator = new();

    [Fact]
    public void ZeroSourceId_Fails() =>
        _validator.Validate(new RaiseCapaRequest(CapaSourceType.Incident, 0, "Retrain staff", 1, new DateOnly(2026, 8, 1)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RaiseCapaRequest(CapaSourceType.Incident, 1, "Retrain staff", 1, new DateOnly(2026, 8, 1)))
            .IsValid.Should().BeTrue();
}
