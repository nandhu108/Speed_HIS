using FluentAssertions;
using Prixo.Application.Features.Opd;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Opd;

public class RegisterOpVisitRequestValidatorTests
{
    private readonly RegisterOpVisitRequestValidator _validator = new();

    [Fact]
    public void ZeroPatientId_Fails() =>
        _validator.Validate(new RegisterOpVisitRequest(0, 1, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RegisterOpVisitRequest(1, 1, null, DepartmentId: 1)).IsValid.Should().BeTrue();

    [Fact]
    public void MissingDepartment_Fails() =>
        _validator.Validate(new RegisterOpVisitRequest(1, 1, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroDepartment_Fails() =>
        _validator.Validate(new RegisterOpVisitRequest(1, 1, null, DepartmentId: 0)).IsValid.Should().BeFalse();

    [Fact]
    public void AdvanceAmount_WithoutPaymentMode_Fails() =>
        _validator.Validate(new RegisterOpVisitRequest(1, 1, null, DepartmentId: 1, AdvanceAmount: 500))
            .IsValid.Should().BeFalse();

    [Fact]
    public void AdvanceAmount_WithPaymentMode_Passes() =>
        _validator.Validate(new RegisterOpVisitRequest(1, 1, null, DepartmentId: 1, AdvanceAmount: 500, PaymentModeId: 1))
            .IsValid.Should().BeTrue();
}

public class RegisterMhcVisitRequestValidatorTests
{
    private readonly RegisterMhcVisitRequestValidator _validator = new();

    [Fact]
    public void ZeroPatientId_Fails() =>
        _validator.Validate(new RegisterMhcVisitRequest(0, 1, 1)).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroMhcDoctorId_Fails() =>
        _validator.Validate(new RegisterMhcVisitRequest(1, 0, 1)).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroMhcPackageId_Fails() =>
        _validator.Validate(new RegisterMhcVisitRequest(1, 1, 0)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RegisterMhcVisitRequest(1, 1, 1)).IsValid.Should().BeTrue();
}

public class SaveVitalsRequestValidatorTests
{
    private readonly SaveVitalsRequestValidator _validator = new();

    [Fact]
    public void AllNull_Passes() =>
        _validator.Validate(new SaveVitalsRequest(null, null, null, null, null, null, null, null, null))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(301)]
    [InlineData(-1)]
    public void OutOfRangePulse_Fails(int pulse) =>
        _validator.Validate(new SaveVitalsRequest(null, null, null, pulse, null, null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidVitals_Pass() =>
        _validator.Validate(new SaveVitalsRequest(170, 68, 98.6m, 76, 16, 120, 80, 98, 100))
            .IsValid.Should().BeTrue();
}

public class SavePrescriptionRequestValidatorTests
{
    private readonly SavePrescriptionRequestValidator _validator = new();

    [Fact]
    public void EmptyItems_Passes() =>
        _validator.Validate(new SavePrescriptionRequest(null, [])).IsValid.Should().BeTrue();

    [Fact]
    public void ItemMissingDose_Fails()
    {
        var request = new SavePrescriptionRequest(null, [new SavePrescriptionItemRequest(1, "", "1-0-1", "5 days", null, null)]);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}

public class AddVisitReferralRequestValidatorTests
{
    private readonly AddVisitReferralRequestValidator _validator = new();

    [Fact]
    public void Internal_WithoutDoctor_Fails()
    {
        var request = new AddVisitReferralRequest(ReferralDirection.Internal, null, null, "Needs specialist opinion");
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Internal_WithDoctor_Passes()
    {
        var request = new AddVisitReferralRequest(ReferralDirection.Internal, 5, null, "Needs specialist opinion");
        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void External_WithoutDetails_Fails()
    {
        var request = new AddVisitReferralRequest(ReferralDirection.External, null, null, "Needs specialist opinion");
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void External_WithDetails_Passes()
    {
        var request = new AddVisitReferralRequest(ReferralDirection.External, null, "City Eye Hospital", "Needs specialist opinion");
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}

public class SaveReferralOpinionRequestValidatorTests
{
    private readonly SaveReferralOpinionRequestValidator _validator = new();

    [Fact]
    public void EmptyOpinion_Fails() =>
        _validator.Validate(new SaveReferralOpinionRequest("")).IsValid.Should().BeFalse();

    [Fact]
    public void WithOpinion_Passes() =>
        _validator.Validate(new SaveReferralOpinionRequest("No acute pathology; continue current management.")).IsValid.Should().BeTrue();
}

public class SavePrescriptionTemplateRequestValidatorTests
{
    private readonly SavePrescriptionTemplateRequestValidator _validator = new();

    [Fact]
    public void EmptyItems_Fails()
    {
        var request = new SavePrescriptionTemplateRequest(1, "URTI Standard", []);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new SavePrescriptionTemplateRequest(
            1, "URTI Standard", [new SaveTemplateItemRequest(1, "1 tab", "1-0-1", "5 days", null, null)]);
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}
