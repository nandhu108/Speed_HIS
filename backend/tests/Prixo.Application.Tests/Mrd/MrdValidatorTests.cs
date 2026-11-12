using FluentAssertions;
using Prixo.Application.Features.Mrd;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Mrd;

public class CreateRecordFileRequestValidatorTests
{
    private readonly CreateRecordFileRequestValidator _validator = new();

    [Fact]
    public void ZeroAdmissionId_Fails() =>
        _validator.Validate(new CreateRecordFileRequest(0)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new CreateRecordFileRequest(1)).IsValid.Should().BeTrue();
}

public class RecordMovementRequestValidatorTests
{
    private readonly RecordMovementRequestValidator _validator = new();

    [Fact]
    public void EmptyToLocation_Fails() =>
        _validator.Validate(new RecordMovementRequest("", null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RecordMovementRequest("Ward 3", null, "Needed for round")).IsValid.Should().BeTrue();
}

public class CreateFileRequestRequestValidatorTests
{
    private readonly CreateFileRequestRequestValidator _validator = new();

    [Fact]
    public void ExternalRequest_WithoutRequesterName_Fails() =>
        _validator.Validate(new CreateFileRequestRequest(1, FileRequestType.External, null, "ABC Insurance", "Claim review", null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ExternalRequest_WithRequesterName_Passes() =>
        _validator.Validate(new CreateFileRequestRequest(1, FileRequestType.External, "Jane Doe", "ABC Insurance", "Claim review", null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void InternalRequest_WithoutRequesterName_Passes() =>
        _validator.Validate(new CreateFileRequestRequest(1, FileRequestType.Internal, null, null, "Doctor review", null))
            .IsValid.Should().BeTrue();
}

public class SaveRetentionPolicyRequestValidatorTests
{
    private readonly SaveRetentionPolicyRequestValidator _validator = new();

    [Fact]
    public void ZeroRetentionYears_Fails() =>
        _validator.Validate(new SaveRetentionPolicyRequest("IP_RECORD", "Inpatient Record", 0, null)).IsValid.Should().BeFalse();

    [Fact]
    public void LowercaseCode_Fails() =>
        _validator.Validate(new SaveRetentionPolicyRequest("ip_record", "Inpatient Record", 10, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new SaveRetentionPolicyRequest("IP_RECORD", "Inpatient Record", 10, null)).IsValid.Should().BeTrue();
}

public class DestroyRequestValidatorTests
{
    private readonly DestroyRequestValidator _validator = new();

    [Fact]
    public void EmptyMethod_Fails() =>
        _validator.Validate(new DestroyRequest("", null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new DestroyRequest("Shredding", "Witnessed by MRD officer")).IsValid.Should().BeTrue();
}
