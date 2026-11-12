using FluentAssertions;
using Prixo.Application.Features.Patients;
using Prixo.Domain.Enums;

namespace Prixo.Application.Tests.Patients;

public class RegisterPatientRequestValidatorTests
{
    private readonly RegisterPatientRequestValidator _validator = new();

    private static RegisterPatientRequest Valid(DateOnly? dob = null, int? estimatedAge = null) =>
        new(
            SalutationId: null, FirstName: "Ravi", MiddleName: null, LastName: "Kumar", Gender: Gender.Male,
            DateOfBirth: dob ?? new DateOnly(1990, 5, 15), EstimatedAge: estimatedAge, BloodGroup: null, MaritalStatusId: null,
            Mobile: "9876543210", AlternateMobile: null, WhatsAppNumber: null, Email: null,
            Address: null, PermanentAddress: null, IsPermanentSameAsPresent: true,
            StateId: null, CityId: null, AreaId: null, Pincode: null, CountryId: null, NationalityId: null,
            Occupation: null, MonthlyIncome: null, LanguageId: null,
            MotherName: null, NationalId: null,
            PatientCaseTypeId: null, PatientHcTagId: null, SpeciallyAbledCategoryId: null,
            IsVip: false, SmsOptIn: true, InsuranceRequired: false, Remarks: null,
            PatientCategoryId: null, ReferrerId: null,
            Contacts: [], Identifiers: [], InsurancePolicies: []);

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void MissingDobAndEstimatedAge_Fails()
    {
        var request = Valid(dob: null) with { DateOfBirth = null, EstimatedAge = null };
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void EstimatedAgeOnly_Passes()
    {
        var request = Valid() with { DateOfBirth = null, EstimatedAge = 40 };
        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abcd")]
    [InlineData("123")]
    public void InvalidMobile_Fails(string mobile) =>
        _validator.Validate(Valid() with { Mobile = mobile }).IsValid.Should().BeFalse();

    [Fact]
    public void InvalidContactEmail_Fails()
    {
        var request = Valid() with
        {
            Contacts = [new SavePatientContactRequest(PatientContactType.Guardian, "Father", null, null, "not-an-email", null)],
        };
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void OtherIdentifier_WithoutDocumentType_Fails()
    {
        var request = Valid() with
        {
            Identifiers = [new SavePatientIdentifierRequest(PatientIdentifierType.Other, null, "AB1234567")],
        };
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void OtherIdentifier_WithDocumentType_Passes()
    {
        var request = Valid() with
        {
            Identifiers = [new SavePatientIdentifierRequest(PatientIdentifierType.Other, 1, "AB1234567")],
        };
        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void InsuranceRequired_WithoutPolicy_Fails()
    {
        var request = Valid() with { InsuranceRequired = true };
        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void InsuranceRequired_WithPolicy_Passes()
    {
        var request = Valid() with
        {
            InsuranceRequired = true,
            InsurancePolicies = [new SavePatientInsurancePolicyRequest(1, "POL-123", null, null)],
        };
        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}

public class CheckDuplicatesRequestValidatorTests
{
    private readonly CheckDuplicatesRequestValidator _validator = new();

    [Fact]
    public void MissingNames_Fails() =>
        _validator.Validate(new CheckDuplicatesRequest("", "", null, null, null)).IsValid.Should().BeFalse();

    [Fact]
    public void NamesOnly_Passes() =>
        _validator.Validate(new CheckDuplicatesRequest("Ravi", "Kumar", null, null, null)).IsValid.Should().BeTrue();
}

public class MergePatientRequestValidatorTests
{
    private readonly MergePatientRequestValidator _validator = new();

    [Fact]
    public void MissingReason_Fails() =>
        _validator.Validate(new MergePatientRequest(2, "")).IsValid.Should().BeFalse();

    [Fact]
    public void ValidRequest_Passes() =>
        _validator.Validate(new MergePatientRequest(2, "Duplicate registration")).IsValid.Should().BeTrue();
}
