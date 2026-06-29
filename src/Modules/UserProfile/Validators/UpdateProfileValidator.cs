using FluentValidation;
using LinkUp.Modules.UserProfile.DTOs;

namespace LinkUp.Modules.UserProfile.Validators;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.Bio).MaximumLength(500).When(x => x.Bio is not null);
        RuleFor(x => x.Gender).MaximumLength(50).When(x => x.Gender is not null);
        RuleFor(x => x.Location).MaximumLength(200).When(x => x.Location is not null);
        RuleFor(x => x.Website).MaximumLength(500)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Website must be a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));
        RuleFor(x => x.Birthday)
            .LessThan(DateTime.UtcNow)
            .WithMessage("Birthday must be in the past.")
            .When(x => x.Birthday.HasValue);
    }
}
