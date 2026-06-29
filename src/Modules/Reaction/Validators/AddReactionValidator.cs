using FluentValidation;
using LinkUp.Modules.Reaction.DTOs;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Reaction.Validators;

public class AddReactionValidator : AbstractValidator<AddReactionDto>
{
    public AddReactionValidator()
    {
        RuleFor(x => x.TargetId)
            .NotEmpty().WithMessage("TargetId must not be empty.");

        RuleFor(x => x.TargetType)
            .NotEmpty().WithMessage("TargetType must not be empty.")
            .Must(t => t == "Post" || t == "Comment")
            .WithMessage("TargetType must be 'Post' or 'Comment'.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Type must be a valid ReactionType value.");
    }
}
