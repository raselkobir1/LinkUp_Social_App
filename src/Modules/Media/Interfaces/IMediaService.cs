using LinkUp.Modules.Media.DTOs;
using Microsoft.AspNetCore.Http;

namespace LinkUp.Modules.Media.Interfaces;

public interface IMediaService
{
    Task<MediaUploadResultDto> UploadImageAsync(IFormFile file, string folder, Guid userId, CancellationToken ct = default);
    Task<MediaUploadResultDto> UploadVideoAsync(IFormFile file, string folder, Guid userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(string publicId, CancellationToken ct = default);
    string GetThumbnailUrl(string publicId, int width, int height);
}
