using FluentValidation;
using LinkUp.Modules.Post.DTOs;

namespace LinkUp.Modules.Post.Validators;

public class SharePostValidator : AbstractValidator<SharePostDto>
{
    public SharePostValidator()
    {
        RuleFor(x => x.OriginalPostId)
            .NotEmpty()
            .WithMessage("Original post ID is required.");

        RuleFor(x => x.Content)
            .MaximumLength(5000)
            .WithMessage("Content must not exceed 5000 characters.");

        RuleFor(x => x.Visibility)
            .IsInEnum()
            .WithMessage("Invalid visibility value.");
    }
}
