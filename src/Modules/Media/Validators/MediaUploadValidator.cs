using FluentValidation;
using LinkUp.SharedKernel.Constants;
using Microsoft.AspNetCore.Http;

namespace LinkUp.Modules.Media.Validators;

public class ImageUploadValidator : AbstractValidator<IFormFile>
{
    public ImageUploadValidator()
    {
        RuleFor(x => x).NotNull().WithMessage("File is required.");
        RuleFor(x => x.Length)
            .LessThanOrEqualTo(AppConstants.Media.MaxImageSizeBytes)
            .WithMessage($"Image size must not exceed {AppConstants.Media.MaxImageSizeBytes / (1024 * 1024)} MB.");
        RuleFor(x => x.ContentType)
            .Must(ct => new[] { "image/jpeg", "image/png", "image/gif", "image/webp" }.Contains(ct.ToLower()))
            .WithMessage("Unsupported image format. Allowed: JPEG, PNG, GIF, WebP.");
    }
}

public class VideoUploadValidator : AbstractValidator<IFormFile>
{
    public VideoUploadValidator()
    {
        RuleFor(x => x).NotNull().WithMessage("File is required.");
        RuleFor(x => x.Length)
            .LessThanOrEqualTo(AppConstants.Media.MaxVideoSizeBytes)
            .WithMessage($"Video size must not exceed {AppConstants.Media.MaxVideoSizeBytes / (1024 * 1024)} MB.");
        RuleFor(x => x.ContentType)
            .Must(ct => new[] { "video/mp4", "video/webm", "video/quicktime", "video/x-msvideo" }.Contains(ct.ToLower()))
            .WithMessage("Unsupported video format. Allowed: MP4, WebM, MOV, AVI.");
    }
}
