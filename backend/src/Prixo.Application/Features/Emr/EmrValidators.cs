using FluentValidation;

namespace Prixo.Application.Features.Emr;

public class AddMedicalHistoryRequestValidator : AbstractValidator<AddMedicalHistoryRequest>
{
    public AddMedicalHistoryRequestValidator()
    {
        RuleFor(x => x.Condition).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class AddSurgicalHistoryRequestValidator : AbstractValidator<AddSurgicalHistoryRequest>
{
    public AddSurgicalHistoryRequestValidator()
    {
        RuleFor(x => x.ProcedureName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Surgeon).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class AddFamilyHistoryRequestValidator : AbstractValidator<AddFamilyHistoryRequest>
{
    public AddFamilyHistoryRequestValidator()
    {
        RuleFor(x => x.Relation).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Condition).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class AddImmunizationRequestValidator : AbstractValidator<AddImmunizationRequest>
{
    public AddImmunizationRequestValidator()
    {
        RuleFor(x => x.VaccineName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AdministeredBy).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class AddAllergyRequestValidator : AbstractValidator<AddAllergyRequest>
{
    public AddAllergyRequestValidator()
    {
        RuleFor(x => x.AllergenType).IsInEnum();
        RuleFor(x => x.AllergenName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Reaction).MaximumLength(500);
        RuleFor(x => x.Severity).IsInEnum();
    }
}

public class AddProblemRequestValidator : AbstractValidator<AddProblemRequest>
{
    public AddProblemRequestValidator()
    {
        RuleFor(x => x.DiagnosisText).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class AddClinicalNoteRequestValidator : AbstractValidator<AddClinicalNoteRequest>
{
    public AddClinicalNoteRequestValidator()
    {
        RuleFor(x => x.NoteType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Title).MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(8000);
    }
}
