using FluentValidation;
using LinkUp.Modules.Chat.DTOs;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Chat.Validators;

public class SendMessageValidator : AbstractValidator<SendMessageDto>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty().WithMessage("ChatId is required.");

        RuleFor(x => x.MessageType)
            .IsInEnum().WithMessage("Invalid MessageType.");

        When(x => x.MessageType == MessageType.Text, () =>
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required for text messages.")
                .MaximumLength(4000).WithMessage("Content must not exceed 4000 characters.");
        });

        When(x => x.MessageType != MessageType.Text, () =>
        {
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.Content) || !string.IsNullOrWhiteSpace(x.AttachmentUrl))
                .WithMessage("Either Content or AttachmentUrl must be provided.");
        });

        When(x => x.AttachmentUrl != null, () =>
        {
            RuleFor(x => x.AttachmentUrl)
                .MaximumLength(500).WithMessage("AttachmentUrl must not exceed 500 characters.");
        });
    }
}
