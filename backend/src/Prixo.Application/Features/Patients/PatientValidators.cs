using FluentValidation;

namespace Prixo.Application.Features.Patients;

public class SavePatientContactRequestValidator : AbstractValidator<SavePatientContactRequest>
{
    public SavePatientContactRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Relationship).MaximumLength(50);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

public class SavePatientIdentifierRequestValidator : AbstractValidator<SavePatientIdentifierRequest>
{
    public SavePatientIdentifierRequestValidator()
    {
        RuleFor(x => x.Value).NotEmpty().MaximumLength(50);
        RuleFor(x => x.IdentityDocumentTypeId).NotNull()
            .When(x => x.Type == Prixo.Domain.Enums.PatientIdentifierType.Other)
            .WithMessage("Select a document type.");
    }
}

public class SavePatientInsurancePolicyRequestValidator : AbstractValidator<SavePatientInsurancePolicyRequest>
{
    public SavePatientInsurancePolicyRequestValidator()
    {
        RuleFor(x => x.InsuranceCompanyId).GreaterThan(0);
        RuleFor(x => x.PolicyNumber).NotEmpty().MaximumLength(50);
    }
}

public class RegisterPatientRequestValidator : AbstractValidator<RegisterPatientRequest>
{
    public RegisterPatientRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BloodGroup).MaximumLength(5);
        RuleFor(x => x.Mobile).NotEmpty().Matches(@"^[0-9+\-\s]{7,20}$")
            .WithMessage("Enter a valid mobile number.");
        RuleFor(x => x.AlternateMobile).Matches(@"^[0-9+\-\s]{7,20}$")
            .When(x => !string.IsNullOrEmpty(x.AlternateMobile))
            .WithMessage("Enter a valid mobile number.");
        RuleFor(x => x.WhatsAppNumber).Matches(@"^[0-9+\-\s]{7,20}$")
            .When(x => !string.IsNullOrEmpty(x.WhatsAppNumber))
            .WithMessage("Enter a valid mobile number.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.PermanentAddress).MaximumLength(500);
        RuleFor(x => x.Pincode).MaximumLength(10);
        RuleFor(x => x.Occupation).MaximumLength(100);
        RuleFor(x => x.MonthlyIncome).GreaterThanOrEqualTo(0).When(x => x.MonthlyIncome is not null);
        RuleFor(x => x.MotherName).MaximumLength(200);
        RuleFor(x => x.NationalId).MaximumLength(50);
        RuleFor(x => x.Remarks).MaximumLength(1000);
        RuleFor(x => x.EstimatedAge).InclusiveBetween(0, 130).When(x => x.EstimatedAge is not null);
        RuleFor(x => x.Gender).IsInEnum().WithMessage("Select a valid gender.");
        RuleFor(x => x.DateOfBirth).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5)))
            .When(x => x.DateOfBirth is not null).WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x)
            .Must(x => x.DateOfBirth is not null || x.EstimatedAge is not null)
            .WithMessage("Provide either a date of birth or an estimated age.");
        RuleForEach(x => x.Contacts).SetValidator(new SavePatientContactRequestValidator());
        RuleForEach(x => x.Identifiers).SetValidator(new SavePatientIdentifierRequestValidator());
        RuleForEach(x => x.InsurancePolicies).SetValidator(new SavePatientInsurancePolicyRequestValidator());
        RuleFor(x => x.InsurancePolicies).NotEmpty()
            .When(x => x.InsuranceRequired)
            .WithMessage("Add at least one insurance policy when insurance is required.");
    }
}

public class UpdatePatientRequestValidator : AbstractValidator<UpdatePatientRequest>
{
    public UpdatePatientRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BloodGroup).MaximumLength(5);
        RuleFor(x => x.Mobile).NotEmpty().Matches(@"^[0-9+\-\s]{7,20}$")
            .WithMessage("Enter a valid mobile number.");
        RuleFor(x => x.AlternateMobile).Matches(@"^[0-9+\-\s]{7,20}$")
            .When(x => !string.IsNullOrEmpty(x.AlternateMobile))
            .WithMessage("Enter a valid mobile number.");
        RuleFor(x => x.WhatsAppNumber).Matches(@"^[0-9+\-\s]{7,20}$")
            .When(x => !string.IsNullOrEmpty(x.WhatsAppNumber))
            .WithMessage("Enter a valid mobile number.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.PermanentAddress).MaximumLength(500);
        RuleFor(x => x.Pincode).MaximumLength(10);
        RuleFor(x => x.Occupation).MaximumLength(100);
        RuleFor(x => x.MonthlyIncome).GreaterThanOrEqualTo(0).When(x => x.MonthlyIncome is not null);
        RuleFor(x => x.MotherName).MaximumLength(200);
        RuleFor(x => x.NationalId).MaximumLength(50);
        RuleFor(x => x.Remarks).MaximumLength(1000);
        RuleFor(x => x.EstimatedAge).InclusiveBetween(0, 130).When(x => x.EstimatedAge is not null);
        RuleFor(x => x.Gender).IsInEnum().WithMessage("Select a valid gender.");
        RuleFor(x => x.DateOfBirth).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5)))
            .When(x => x.DateOfBirth is not null).WithMessage("Date of birth cannot be in the future.");
        RuleFor(x => x)
            .Must(x => x.DateOfBirth is not null || x.EstimatedAge is not null)
            .WithMessage("Provide either a date of birth or an estimated age.");
        RuleForEach(x => x.Contacts).SetValidator(new SavePatientContactRequestValidator());
        RuleForEach(x => x.Identifiers).SetValidator(new SavePatientIdentifierRequestValidator());
        RuleForEach(x => x.InsurancePolicies).SetValidator(new SavePatientInsurancePolicyRequestValidator());
        RuleFor(x => x.InsurancePolicies).NotEmpty()
            .When(x => x.InsuranceRequired)
            .WithMessage("Add at least one insurance policy when insurance is required.");
    }
}

public class CheckDuplicatesRequestValidator : AbstractValidator<CheckDuplicatesRequest>
{
    public CheckDuplicatesRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public class MergePatientRequestValidator : AbstractValidator<MergePatientRequest>
{
    public MergePatientRequestValidator()
    {
        RuleFor(x => x.MergedPatientId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class BlockPatientRequestValidator : AbstractValidator<BlockPatientRequest>
{
    public BlockPatientRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class AddPatientDocumentRequestValidator : AbstractValidator<AddPatientDocumentRequest>
{
    public AddPatientDocumentRequestValidator()
    {
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CaptureConsentRequestValidator : AbstractValidator<CaptureConsentRequest>
{
    public CaptureConsentRequestValidator()
    {
        RuleFor(x => x.ConsentTemplateId).GreaterThan(0);
        RuleFor(x => x.SignedByName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RelationshipToPatient).MaximumLength(50);
    }
}
