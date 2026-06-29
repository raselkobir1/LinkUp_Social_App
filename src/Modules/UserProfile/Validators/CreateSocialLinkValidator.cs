using FluentValidation;
using LinkUp.Modules.UserProfile.DTOs;

namespace LinkUp.Modules.UserProfile.Validators;

public class CreateSocialLinkValidator : AbstractValidator<CreateSocialLinkDto>
{
    public CreateSocialLinkValidator()
    {
        RuleFor(x => x.Platform).IsInEnum();
        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(500)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Url must be a valid absolute URL.");
    }
}
