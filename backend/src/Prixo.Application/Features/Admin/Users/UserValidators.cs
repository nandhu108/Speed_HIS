using FluentValidation;

namespace Prixo.Application.Features.Admin.Users;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(100)
            .Matches("^[a-zA-Z0-9._@-]+$").WithMessage("Username may contain letters, digits, dot, underscore, hyphen and @ only.");
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.RoleIds).NotEmpty().WithMessage("At least one role is required.");
        RuleFor(x => x.BranchIds).NotEmpty().WithMessage("At least one branch is required.");
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.RoleIds).NotEmpty().WithMessage("At least one role is required.");
        RuleFor(x => x.BranchIds).NotEmpty().WithMessage("At least one branch is required.");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty();
    }
}
