using FluentValidation;
using LinkUp.Modules.Friend.DTOs;

namespace LinkUp.Modules.Friend.Validators;

public class SendFriendRequestValidator : AbstractValidator<SendFriendRequestDto>
{
    public SendFriendRequestValidator()
    {
        RuleFor(x => x.ReceiverId).NotEmpty().WithMessage("Receiver ID is required.");
    }
}
