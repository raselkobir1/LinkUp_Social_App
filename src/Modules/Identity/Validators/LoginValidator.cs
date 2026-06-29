using FluentValidation;
using LinkUp.Modules.Identity.DTOs;

namespace LinkUp.Modules.Identity.Validators;

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
