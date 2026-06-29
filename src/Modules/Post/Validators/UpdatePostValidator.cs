using FluentValidation;
using LinkUp.Modules.Post.DTOs;

namespace LinkUp.Modules.Post.Validators;

public class UpdatePostValidator : AbstractValidator<UpdatePostDto>
{
    public UpdatePostValidator()
    {
        RuleFor(x => x.Content)
            .MaximumLength(5000)
            .WithMessage("Content must not exceed 5000 characters.");

        RuleFor(x => x.Visibility)
            .IsInEnum()
            .WithMessage("Invalid visibility value.");
    }
}
