using FluentAssertions;
using Prixo.Application.Features.Ipd;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Ipd;

public class AdmitPatientRequestValidatorTests
{
    private readonly AdmitPatientRequestValidator _validator = new();

    private static AdmitPatientRequest Valid() => new(
        PatientId: 1, PrimaryConsultantId: 1, BedId: 1, AdmissionType: AdmissionType.Planned,
        ProvisionalDiagnosis: null, ReferringOpVisitId: null, ReferringEmergencyVisitId: null, SecondaryConsultantIds: [],
        ConsentTemplateId: null, ConsentSignedByName: null, ConsentRelationshipToPatient: null,
        InitialDepositAmount: 0, DepositPaymentModeId: null, DepositReferenceNumber: null, DepartmentId: 1);

    [Fact]
    public void ValidRequest_Passes() => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void MissingDepartment_Fails() =>
        _validator.Validate(Valid() with { DepartmentId = null }).IsValid.Should().BeFalse();

    [Fact]
    public void ZeroBedId_Fails() => _validator.Validate(Valid() with { BedId = 0 }).IsValid.Should().BeFalse();

    [Fact]
    public void NegativeDeposit_Fails() => _validator.Validate(Valid() with { InitialDepositAmount = -1 }).IsValid.Should().BeFalse();

    [Fact]
    public void ConsentTemplateWithoutSignedByName_Fails() =>
        _validator.Validate(Valid() with { ConsentTemplateId = 1, ConsentSignedByName = null }).IsValid.Should().BeFalse();

    [Fact]
    public void ConsentTemplateWithSignedByName_Passes() =>
        _validator.Validate(Valid() with { ConsentTemplateId = 1, ConsentSignedByName = "Self" }).IsValid.Should().BeTrue();
}

public class TransferBedRequestValidatorTests
{
    private readonly TransferBedRequestValidator _validator = new();

    [Fact]
    public void ZeroBedId_Fails() => _validator.Validate(new TransferBedRequest(0, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() => _validator.Validate(new TransferBedRequest(2, "Ward change")).IsValid.Should().BeTrue();
}

public class CreateDoctorOrderRequestValidatorTests
{
    private readonly CreateDoctorOrderRequestValidator _validator = new();

    [Fact]
    public void EmptyDescription_Fails() =>
        _validator.Validate(new CreateDoctorOrderRequest(1, DoctorOrderType.Nursing, "", ClinicalOrderPriority.Routine, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new CreateDoctorOrderRequest(1, DoctorOrderType.Nursing, "Reposition every 2h", ClinicalOrderPriority.Routine, null, null, null))
            .IsValid.Should().BeTrue();
}

public class MedicationOrderRequestValidatorTests
{
    private readonly MedicationOrderRequestValidator _validator = new();

    [Fact]
    public void ZeroDrugId_Fails() =>
        _validator.Validate(new MedicationOrderRequest(0, "500mg", "Oral", "TDS", new DateOnly(2027, 1, 1), null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new MedicationOrderRequest(1, "500mg", "Oral", "TDS", new DateOnly(2027, 1, 1), null, null))
            .IsValid.Should().BeTrue();
}

public class RequestDischargeRequestValidatorTests
{
    private readonly RequestDischargeRequestValidator _validator = new();

    [Fact]
    public void ZeroDoctorId_Fails() =>
        _validator.Validate(new RequestDischargeRequest(0, DischargeType.Normal, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new RequestDischargeRequest(1, DischargeType.Normal, null)).IsValid.Should().BeTrue();
}

public class UpsertDischargeSummaryRequestValidatorTests
{
    private readonly UpsertDischargeSummaryRequestValidator _validator = new();

    [Fact]
    public void EmptyFinalDiagnosis_Fails() =>
        _validator.Validate(new UpsertDischargeSummaryRequest("", null, null, null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new UpsertDischargeSummaryRequest("Resolved fever", null, null, null, null)).IsValid.Should().BeTrue();
}

public class AddIntakeOutputRequestValidatorTests
{
    private readonly AddIntakeOutputRequestValidator _validator = new();

    [Fact]
    public void ZeroQuantity_Fails() =>
        _validator.Validate(new AddIntakeOutputRequest(true, "Oral", 0, null)).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new AddIntakeOutputRequest(true, "Oral", 200, null)).IsValid.Should().BeTrue();
}

public class SetBedStatusRequestValidatorTests
{
    private readonly SetBedStatusRequestValidator _validator = new();

    [Fact]
    public void ValidStatus_Passes() =>
        _validator.Validate(new SetBedStatusRequest(BedStatus.Cleaning, null)).IsValid.Should().BeTrue();
}

public class CancelAdmissionRequestValidatorTests
{
    private readonly CancelAdmissionRequestValidator _validator = new();

    [Fact]
    public void EmptyReason_Fails() => _validator.Validate(new CancelAdmissionRequest("")).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() => _validator.Validate(new CancelAdmissionRequest("Patient refused admission")).IsValid.Should().BeTrue();
}
