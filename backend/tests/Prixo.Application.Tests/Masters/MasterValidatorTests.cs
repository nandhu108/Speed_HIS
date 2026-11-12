using FluentAssertions;
using Prixo.Application.Features.Masters.Billing;
using Prixo.Application.Features.Masters.Doctors;
using Prixo.Application.Features.Masters.Packages;
using Prixo.Application.Features.Masters.Referrers;
using Prixo.Application.Features.Masters.Tariffs;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Masters;

public class TariffValidatorTests
{
    private readonly SaveTariffRequestValidator _validator = new();

    private static SaveTariffRequest Valid(DateOnly? to = null, IReadOnlyList<SaveTariffItemRequest>? items = null) =>
        new("TRF-2026", "General Tariff 2026", null, new DateOnly(2026, 1, 1), to,
            items ?? [new SaveTariffItemRequest(1, 500)]);

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void EffectiveTo_BeforeEffectiveFrom_Fails() =>
        _validator.Validate(Valid(to: new DateOnly(2025, 12, 31))).IsValid.Should().BeFalse();

    [Fact]
    public void DuplicateService_Fails()
    {
        var request = Valid(items: [new SaveTariffItemRequest(1, 100), new SaveTariffItemRequest(1, 200)]);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeRate_Fails()
    {
        var request = Valid(items: [new SaveTariffItemRequest(1, -1)]);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}

public class PackageValidatorTests
{
    private readonly SavePackageRequestValidator _validator = new();

    [Fact]
    public void EmptyItems_Fails()
    {
        var request = new SavePackageRequest("PKG1", "Health Check", null, 1000, 30, []);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ZeroQuantity_Fails()
    {
        var request = new SavePackageRequest("PKG1", "Health Check", null, 1000, 30,
            [new SavePackageItemRequest(1, 0)]);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}

public class DiscountTypeValidatorTests
{
    private readonly SaveDiscountTypeRequestValidator _validator = new();

    [Fact]
    public void ApprovalThreshold_AboveMaxPercent_Fails()
    {
        var request = new SaveDiscountTypeRequest("STAFF", "Staff Discount", 20, 30);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PercentOver100_Fails()
    {
        var request = new SaveDiscountTypeRequest("STAFF", "Staff Discount", 120, 10);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}

public class BillEstimateTemplateValidatorTests
{
    private readonly SaveBillEstimateTemplateRequestValidator _validator = new();

    [Fact]
    public void EmptyItems_Fails()
    {
        var request = new SaveBillEstimateTemplateRequest("EST1", "Normal Delivery Estimate", null, []);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ServiceLine_WithoutServiceId_Fails()
    {
        var request = new SaveBillEstimateTemplateRequest("EST1", "Normal Delivery Estimate", null,
            [new SaveBillEstimateTemplateItemRequest(BillItemSourceType.Service, null, null, 1)]);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new SaveBillEstimateTemplateRequest("EST1", "Normal Delivery Estimate", null,
            [new SaveBillEstimateTemplateItemRequest(BillItemSourceType.Package, null, 1, 1)]);
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}

public class DoctorPayoutRuleValidatorTests
{
    private readonly SaveDoctorPayoutRuleRequestValidator _validator = new();

    [Fact]
    public void PercentOver100_Fails()
    {
        var request = new SaveDoctorPayoutRuleRequest(1, 1, PayoutType.Percent, 120);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void FlatOver100_Passes()
    {
        var request = new SaveDoctorPayoutRuleRequest(1, 1, PayoutType.Flat, 500);
        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void NegativeValue_Fails()
    {
        var request = new SaveDoctorPayoutRuleRequest(1, 1, PayoutType.Flat, -1);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}

public class DoctorValidatorTests
{
    private readonly SaveDoctorRequestValidator _validator = new();

    [Fact]
    public void NoDepartments_Fails()
    {
        var request = new SaveDoctorRequest(1, "REG123", null, null, 500, 300, 14, 10, []);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new SaveDoctorRequest(1, "REG123", null, "MBBS", 500, 300, 14, 10, [1]);
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}

public class ReferrerValidatorTests
{
    private readonly SaveReferrerRequestValidator _validator = new();

    [Fact]
    public void InternalReferrer_WithoutDoctor_Fails()
    {
        var request = new SaveReferrerRequest("REF1", "Dr. Internal", ReferrerType.Internal, null, null, null);
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ExternalReferrer_WithoutDoctor_Passes()
    {
        var request = new SaveReferrerRequest("REF1", "City Clinic", ReferrerType.External, null, null, "City Clinic");
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}
