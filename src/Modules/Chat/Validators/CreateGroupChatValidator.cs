using FluentValidation;
using LinkUp.Modules.Chat.DTOs;

namespace LinkUp.Modules.Chat.Validators;

public class CreateGroupChatValidator : AbstractValidator<CreateGroupChatDto>
{
    public CreateGroupChatValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MaximumLength(200).WithMessage("Group name must not exceed 200 characters.");

        RuleFor(x => x.MemberIds)
            .NotEmpty().WithMessage("At least one member must be specified.")
            .Must(ids => ids.Count > 0).WithMessage("MemberIds cannot be empty.");

        RuleForEach(x => x.MemberIds)
            .NotEmpty().WithMessage("Each member ID must be a valid non-empty GUID.");
    }
}
