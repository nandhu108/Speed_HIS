using FluentAssertions;
using Prixo.Application.Features.Insurance;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Insurance;

public class RaisePreAuthRequestValidatorTests
{
    private readonly RaisePreAuthRequestValidator _validator = new();

    [Fact]
    public void OpTreatment_WithoutOpVisitId_Fails() =>
        _validator.Validate(new RaisePreAuthRequest(1, 1, PreAuthTreatmentType.Op, null, null, 5000, "Fracture"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void OpTreatment_WithOpVisitId_Passes() =>
        _validator.Validate(new RaisePreAuthRequest(1, 1, PreAuthTreatmentType.Op, 10, null, 5000, "Fracture"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void IpTreatment_WithoutAdmissionId_Fails() =>
        _validator.Validate(new RaisePreAuthRequest(1, 1, PreAuthTreatmentType.Ip, null, null, 50000, "Surgery"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ZeroEstimatedAmount_Fails() =>
        _validator.Validate(new RaisePreAuthRequest(1, 1, PreAuthTreatmentType.Ip, null, 10, 0, "Surgery"))
            .IsValid.Should().BeFalse();
}

public class DecidePreAuthRequestValidatorTests
{
    private readonly DecidePreAuthRequestValidator _validator = new();

    [Fact]
    public void Approve_WithoutAmount_Fails() =>
        _validator.Validate(new DecidePreAuthRequest(true, null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void Approve_WithAmount_Passes() =>
        _validator.Validate(new DecidePreAuthRequest(true, 5000, null)).IsValid.Should().BeTrue();

    [Fact]
    public void Reject_WithoutReason_Fails() =>
        _validator.Validate(new DecidePreAuthRequest(false, null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void Reject_WithReason_Passes() =>
        _validator.Validate(new DecidePreAuthRequest(false, null, "Policy lapsed")).IsValid.Should().BeTrue();
}

public class SubmitClaimRequestValidatorTests
{
    private readonly SubmitClaimRequestValidator _validator = new();

    [Fact]
    public void ZeroBillId_Fails() =>
        _validator.Validate(new SubmitClaimRequest(1, 1, null, 0, 1000)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SubmitClaimRequest(1, 1, null, 5, 1000)).IsValid.Should().BeTrue();
}

public class UpdateClaimStatusRequestValidatorTests
{
    private readonly UpdateClaimStatusRequestValidator _validator = new();

    [Fact]
    public void Approved_WithoutAmount_Fails() =>
        _validator.Validate(new UpdateClaimStatusRequest(ClaimStatus.Approved, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Denied_WithoutReason_Fails() =>
        _validator.Validate(new UpdateClaimStatusRequest(ClaimStatus.Denied, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Denied_WithReason_Passes() =>
        _validator.Validate(new UpdateClaimStatusRequest(ClaimStatus.Denied, null, 1, "Non-payable item"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void UnderReview_Passes() =>
        _validator.Validate(new UpdateClaimStatusRequest(ClaimStatus.UnderReview, null, null, null))
            .IsValid.Should().BeTrue();
}

public class RecordClaimSettlementRequestValidatorTests
{
    private readonly RecordClaimSettlementRequestValidator _validator = new();

    [Fact]
    public void ZeroSettledAmount_Fails() =>
        _validator.Validate(new RecordClaimSettlementRequest(0, null, 1, "UTR123")).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RecordClaimSettlementRequest(4500, 500, 1, "UTR123")).IsValid.Should().BeTrue();
}
