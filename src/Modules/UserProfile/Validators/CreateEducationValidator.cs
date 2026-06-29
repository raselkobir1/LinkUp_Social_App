using FluentValidation;
using LinkUp.Modules.UserProfile.DTOs;

namespace LinkUp.Modules.UserProfile.Validators;

public class CreateEducationValidator : AbstractValidator<CreateEducationDto>
{
    public CreateEducationValidator()
    {
        RuleFor(x => x.School).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Degree).MaximumLength(200).When(x => x.Degree is not null);
        RuleFor(x => x.FieldOfStudy).MaximumLength(200).When(x => x.FieldOfStudy is not null);
        RuleFor(x => x.StartYear).InclusiveBetween(1900, DateTime.UtcNow.Year);
        RuleFor(x => x.EndYear)
            .GreaterThanOrEqualTo(x => x.StartYear)
            .WithMessage("End year must be after start year.")
            .When(x => x.EndYear.HasValue);
    }
}

public class UpdateEducationValidator : AbstractValidator<UpdateEducationDto>
{
    public UpdateEducationValidator()
    {
        RuleFor(x => x.School).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Degree).MaximumLength(200).When(x => x.Degree is not null);
        RuleFor(x => x.FieldOfStudy).MaximumLength(200).When(x => x.FieldOfStudy is not null);
        RuleFor(x => x.StartYear).InclusiveBetween(1900, DateTime.UtcNow.Year);
        RuleFor(x => x.EndYear)
            .GreaterThanOrEqualTo(x => x.StartYear)
            .WithMessage("End year must be after start year.")
            .When(x => x.EndYear.HasValue);
    }
}
