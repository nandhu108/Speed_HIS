using FluentValidation;

namespace Prixo.Application.Features.Loyalty;

public class RegisterLoyaltyCardRequestValidator : AbstractValidator<RegisterLoyaltyCardRequest>
{
    public RegisterLoyaltyCardRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.LoyaltyCardTypeId).GreaterThan(0);
        RuleFor(x => x.DurationMonths).GreaterThan(0);
    }
}

public class RenewLoyaltyCardRequestValidator : AbstractValidator<RenewLoyaltyCardRequest>
{
    public RenewLoyaltyCardRequestValidator()
    {
        RuleFor(x => x.DurationMonths).GreaterThan(0);
    }
}

public class AddDependentRequestValidator : AbstractValidator<AddDependentRequest>
{
    public AddDependentRequestValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.MemberRelationTagId).GreaterThan(0);
    }
}

public class DeactivateLoyaltyCardRequestValidator : AbstractValidator<DeactivateLoyaltyCardRequest>
{
    public DeactivateLoyaltyCardRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
