using Asp.Versioning;
using LinkUp.BuildingBlocks.Common.Controllers;
using LinkUp.Modules.Media.Interfaces;
using LinkUp.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Modules.Media.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/media")]
[Authorize]
public class MediaController(IMediaService mediaService) : BaseApiController
{
    // POST api/v1/media/upload/image
    [HttpPost("upload/image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file, CancellationToken ct)
    {
        var result = await mediaService.UploadImageAsync(file, AppConstants.Media.PostMediaFolder, CurrentUserId, ct);
        return ApiCreated(result, "Image uploaded successfully.");
    }

    // POST api/v1/media/upload/video
    [HttpPost("upload/video")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> UploadVideo([FromForm] IFormFile file, CancellationToken ct)
    {
        var result = await mediaService.UploadVideoAsync(file, AppConstants.Media.PostMediaFolder, CurrentUserId, ct);
        return ApiCreated(result, "Video uploaded successfully.");
    }

    // DELETE api/v1/media/{publicId}
    [HttpDelete("{publicId}")]
    public async Task<IActionResult> DeleteFile(string publicId, CancellationToken ct)
    {
        var deleted = await mediaService.DeleteAsync(publicId, ct);
        if (!deleted)
            return ApiNotFound("Media file not found or could not be deleted.");

        return ApiOk<object>(null!, "Media file deleted successfully.");
    }
}
