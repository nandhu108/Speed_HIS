using FluentValidation;
using Prixo.Domain.Enums;

namespace Prixo.Application.Features.Masters.Departments;

/// <summary>Same shape as the generic SimpleMasterDto (so every existing department dropdown keeps
/// working) plus the Clinical / Non-Clinical type.</summary>
public record DepartmentDto(long Id, string Code, string Name, string? Description, bool IsActive, DepartmentType Type);

public record SaveDepartmentRequest(
    string Code, string Name, string? Description, DepartmentType Type = DepartmentType.Clinical, bool IsActive = true);

public class SaveDepartmentRequestValidator : AbstractValidator<SaveDepartmentRequest>
{
    public SaveDepartmentRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code must be uppercase letters, digits, '-' or '_'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Type).IsInEnum().WithMessage("Select the department type (Clinical or Non-Clinical).");
    }
}
