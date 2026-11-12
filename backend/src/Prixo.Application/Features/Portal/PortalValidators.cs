using FluentValidation;

namespace Prixo.Application.Features.Portal;

public class RegisterPortalAccountRequestValidator : AbstractValidator<RegisterPortalAccountRequest>
{
    public RegisterPortalAccountRequestValidator()
    {
        RuleFor(x => x.Uhid).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Mobile).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class PortalLoginRequestValidator : AbstractValidator<PortalLoginRequest>
{
    public PortalLoginRequestValidator()
    {
        RuleFor(x => x.UhidOrMobile).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class PortalChangePasswordRequestValidator : AbstractValidator<PortalChangePasswordRequest>
{
    public PortalChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
