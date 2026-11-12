using FluentValidation;

namespace Prixo.Application.Features.Masters.PatientCategories;

public record PatientCategoryDto(long Id, string Code, string Name, string? Description, string? ColorHex, bool IsActive);

public record SavePatientCategoryRequest(string Code, string Name, string? Description, string? ColorHex, bool IsActive = true);

public class SavePatientCategoryRequestValidator : AbstractValidator<SavePatientCategoryRequest>
{
    public SavePatientCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code must be uppercase letters, digits, '-' or '_'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ColorHex).Matches("^#[0-9A-Fa-f]{6}$")
            .When(x => !string.IsNullOrEmpty(x.ColorHex))
            .WithMessage("Color must be a hex value like #3B82F6.");
    }
}
