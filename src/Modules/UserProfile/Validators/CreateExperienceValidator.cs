using FluentValidation;
using LinkUp.Modules.UserProfile.DTOs;

namespace LinkUp.Modules.UserProfile.Validators;

public class CreateExperienceValidator : AbstractValidator<CreateExperienceDto>
{
    public CreateExperienceValidator()
    {
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty().LessThanOrEqualTo(DateTime.UtcNow);
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.")
            .When(x => x.EndDate.HasValue);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
    }
}

public class UpdateExperienceValidator : AbstractValidator<UpdateExperienceDto>
{
    public UpdateExperienceValidator()
    {
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty().LessThanOrEqualTo(DateTime.UtcNow);
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.")
            .When(x => x.EndDate.HasValue);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
    }
}
