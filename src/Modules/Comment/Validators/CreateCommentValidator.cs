using FluentValidation;
using LinkUp.Modules.Comment.DTOs;

namespace LinkUp.Modules.Comment.Validators;

public class CreateCommentValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentValidator()
    {
        RuleFor(x => x.PostId)
            .NotEmpty().WithMessage("Post ID is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(2000).WithMessage("Content must not exceed 2000 characters.");
    }
}
