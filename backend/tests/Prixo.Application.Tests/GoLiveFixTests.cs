using FluentAssertions;
using Prixo.Application.Features.Billing;
using Prixo.Application.Features.Ipd;
using Prixo.Application.Features.Nursing;
using Prixo.Application.Features.Ot;
using Prixo.Application.Features.Patients;
using Prixo.Application.Features.Pharmacy;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests;

/// <summary>Regression tests for the go-live QA defects (see qa-reports/QA_Summary.md).</summary>
public class GoLiveFixTests
{
    [Fact]
    public void Discount_without_reason_is_rejected() =>
        new ApplyItemDiscountRequestValidator().Validate(new ApplyItemDiscountRequest(1, 5, "")).IsValid.Should().BeFalse();

    [Fact]
    public void Bill_discount_without_reason_is_rejected() =>
        new ApplyBillDiscountRequestValidator().Validate(new ApplyBillDiscountRequest(1, 5, null)).IsValid.Should().BeFalse();

    [Fact]
    public void Mrp_line_never_exceeds_quantity_times_mrp()
    {
        // 10 units at MRP 15 with 12% GST: patient pays 150, of which GST is 150 - 150/1.12 = 16.07.
        var (discount, tax, total) = StockMath.ComputeMrpInclusiveLineAmounts(15, 10, 0, 12);
        discount.Should().Be(0);
        total.Should().Be(150);
        tax.Should().Be(16.07m);
    }

    [Fact]
    public void Mrp_line_discount_reduces_the_total_and_the_included_tax()
    {
        var (discount, tax, total) = StockMath.ComputeMrpInclusiveLineAmounts(100, 2, 10, 5);
        discount.Should().Be(20);
        total.Should().Be(180);
        tax.Should().Be(8.57m);
    }

    [Theory]
    [InlineData(-30, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void Batch_is_usable_only_before_its_expiry_date(int daysFromToday, bool usable)
    {
        var today = new DateOnly(2026, 11, 13);
        StockMath.IsUsable(today.AddDays(daysFromToday), today).Should().Be(usable);
    }

    [Fact]
    public void Medication_order_without_dose_is_rejected()
    {
        var request = new CreateDoctorOrderRequest(1, DoctorOrderType.Medication, "Inj X", ClinicalOrderPriority.Routine, null,
            new MedicationOrderRequest(5, "", "IV", "BD", new DateOnly(2026, 11, 13), null, null), null);
        new CreateDoctorOrderRequestValidator().Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Medication_order_ending_before_it_starts_is_rejected()
    {
        var request = new CreateDoctorOrderRequest(1, DoctorOrderType.Medication, "Inj X", ClinicalOrderPriority.Routine, null,
            new MedicationOrderRequest(5, "1 g", "IV", "BD", new DateOnly(2026, 11, 13), new DateOnly(2026, 11, 1), null), null);
        new CreateDoctorOrderRequestValidator().Validate(request).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(140, 98, 15)]   // SpO2 > 100
    [InlineData(98, 150, 15)]   // temperature 150F
    [InlineData(98, 98.6, 20)]  // GCS > 15
    public void Implausible_ip_vitals_are_rejected(int spo2, double tempF, int gcs)
    {
        var request = new AddVitalsRequest((decimal)tempF, 80, 18, 120, 80, spo2, null, gcs, null, null, null);
        new AddVitalsRequestValidator().Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Morse_item_outside_scale_is_rejected() =>
        new Prixo.Application.Features.Nursing.AddRiskAssessmentRequestValidator()
            .Validate(new AddRiskAssessmentRequest(RiskAssessmentType.FallRisk, new Dictionary<string, int> { ["historyOfFalling"] = 999 }))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Implant_without_serial_number_is_rejected() =>
        new RecordImplantUsageRequestValidator().Validate(new RecordImplantUsageRequest(1, "", 1)).IsValid.Should().BeFalse();
}
