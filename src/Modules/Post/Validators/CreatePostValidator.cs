using FluentValidation;
using LinkUp.Modules.Post.DTOs;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Post.Validators;

public class CreatePostValidator : AbstractValidator<CreatePostDto>
{
    public CreatePostValidator()
    {
        RuleFor(x => x.Content)
            .MaximumLength(5000)
            .WithMessage("Content must not exceed 5000 characters.");

        RuleFor(x => x.Visibility)
            .IsInEnum()
            .WithMessage("Invalid visibility value.");

        RuleFor(x => x.PostType)
            .IsInEnum()
            .WithMessage("Invalid post type value.");

        When(x => x.PostType == PostType.Image, () =>
        {
            RuleFor(x => x.ImageUrls)
                .NotEmpty()
                .WithMessage("At least one image URL is required for an image post.");
        });

        When(x => x.PostType == PostType.Video, () =>
        {
            RuleFor(x => x.VideoUrl)
                .NotEmpty()
                .WithMessage("A video URL is required for a video post.");
        });
    }
}
