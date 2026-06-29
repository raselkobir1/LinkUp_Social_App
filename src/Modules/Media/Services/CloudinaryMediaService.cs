using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.Modules.Media.Configuration;
using LinkUp.Modules.Media.DTOs;
using LinkUp.Modules.Media.Entities;
using LinkUp.Modules.Media.Interfaces;
using LinkUp.SharedKernel.Constants;
using LinkUp.SharedKernel.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.Media.Services;

public class CloudinaryMediaService(Cloudinary cloudinary, MediaDbContext db) : IMediaService
{
    public async Task<MediaUploadResultDto> UploadImageAsync(IFormFile file, string folder, Guid userId, CancellationToken ct = default)
    {
        if (file.Length > AppConstants.Media.MaxImageSizeBytes)
            throw new ValidationException($"Image size must not exceed {AppConstants.Media.MaxImageSizeBytes / (1024 * 1024)} MB.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            throw new ValidationException("Unsupported image format. Allowed: JPEG, PNG, GIF, WebP.");

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await cloudinary.UploadAsync(uploadParams, ct);

        if (result.Error is not null)
            throw new AppException($"Cloudinary upload failed: {result.Error.Message}");

        var mediaFile = new MediaFile
        {
            UserId = userId,
            PublicId = result.PublicId,
            Url = result.SecureUrl.ToString(),
            FileType = MediaFileType.Image,
            Format = result.Format,
            SizeInBytes = result.Bytes,
            Width = result.Width,
            Height = result.Height,
            Folder = folder
        };

        db.MediaFiles.Add(mediaFile);
        await db.SaveChangesAsync(ct);

        return new MediaUploadResultDto
        {
            Id = mediaFile.Id,
            PublicId = mediaFile.PublicId,
            Url = mediaFile.Url,
            FileType = MediaFileType.Image,
            Format = mediaFile.Format,
            Width = mediaFile.Width,
            Height = mediaFile.Height
        };
    }

    public async Task<MediaUploadResultDto> UploadVideoAsync(IFormFile file, string folder, Guid userId, CancellationToken ct = default)
    {
        if (file.Length > AppConstants.Media.MaxVideoSizeBytes)
            throw new ValidationException($"Video size must not exceed {AppConstants.Media.MaxVideoSizeBytes / (1024 * 1024)} MB.");

        var allowedTypes = new[] { "video/mp4", "video/webm", "video/quicktime", "video/x-msvideo" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            throw new ValidationException("Unsupported video format. Allowed: MP4, WebM, MOV, AVI.");

        await using var stream = file.OpenReadStream();

        var uploadParams = new VideoUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await cloudinary.UploadAsync(uploadParams, ct);

        if (result.Error is not null)
            throw new AppException($"Cloudinary upload failed: {result.Error.Message}");

        var thumbnailUrl = GetThumbnailUrl(result.PublicId, 320, 180);

        var mediaFile = new MediaFile
        {
            UserId = userId,
            PublicId = result.PublicId,
            Url = result.SecureUrl.ToString(),
            ThumbnailUrl = thumbnailUrl,
            FileType = MediaFileType.Video,
            Format = result.Format,
            SizeInBytes = result.Bytes,
            Width = result.Width,
            Height = result.Height,
            Duration = result.Duration,
            Folder = folder
        };

        db.MediaFiles.Add(mediaFile);
        await db.SaveChangesAsync(ct);

        return new MediaUploadResultDto
        {
            Id = mediaFile.Id,
            PublicId = mediaFile.PublicId,
            Url = mediaFile.Url,
            ThumbnailUrl = mediaFile.ThumbnailUrl,
            FileType = MediaFileType.Video,
            Format = mediaFile.Format,
            Width = mediaFile.Width,
            Height = mediaFile.Height,
            Duration = mediaFile.Duration
        };
    }

    public async Task<bool> DeleteAsync(string publicId, CancellationToken ct = default)
    {
        var mediaFile = await db.MediaFiles
            .FirstOrDefaultAsync(m => m.PublicId == publicId && !m.IsDeleted, ct);

        // Attempt Cloudinary deletion regardless of DB record
        var deleteParams = new DeletionParams(publicId);
        var result = await cloudinary.DestroyAsync(deleteParams);

        if (mediaFile is not null)
        {
            mediaFile.IsDeleted = true;
            mediaFile.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return result.Result == "ok";
    }

    public string GetThumbnailUrl(string publicId, int width, int height)
    {
        var transformation = new Transformation()
            .Width(width)
            .Height(height)
            .Crop("fill")
            .Quality("auto")
            .FetchFormat("auto");

        return cloudinary.Api.UrlImgUp
            .ResourceType("video")
            .Transform(transformation)
            .BuildUrl($"{publicId}.jpg");
    }
}
