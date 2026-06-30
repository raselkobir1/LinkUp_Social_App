using Asp.Versioning;
using LinkUp.BuildingBlocks.Common.Controllers;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Post.DTOs;
using LinkUp.Modules.Post.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Modules.Post.Controllers;

[ApiVersion("1.0")]
public class PostsController(IPostManager postManager) : BaseApiController
{
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto, CancellationToken ct)
    {
        var result = await postManager.CreatePostAsync(CurrentUserId, dto, ct);
        return ApiCreated(result, "Post created successfully.");
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdatePost(Guid id, [FromBody] UpdatePostDto dto, CancellationToken ct)
    {
        var result = await postManager.UpdatePostAsync(CurrentUserId, id, dto, ct);
        return ApiOk(result, "Post updated successfully.");
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken ct)
    {
        await postManager.DeletePostAsync(CurrentUserId, id, ct);
        return ApiOk<object>(null!, "Post deleted successfully.");
    }

    [HttpPatch("{id:guid}/pin")]
    [Authorize]
    public async Task<IActionResult> PinPost(Guid id, [FromQuery] bool pin, CancellationToken ct)
    {
        var result = await postManager.PinPostAsync(CurrentUserId, id, pin, ct);
        return ApiOk(result, pin ? "Post pinned successfully." : "Post unpinned successfully.");
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPost(Guid id, CancellationToken ct)
    {
        var viewerId = IsAuthenticated ? CurrentUserId : Guid.Empty;
        var result = await postManager.GetPostByIdAsync(id, viewerId, ct);
        return ApiOk(result);
    }

    [HttpGet("wall/{userId:guid}")]
    public async Task<IActionResult> GetWallPosts(Guid userId, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var viewerId = IsAuthenticated ? CurrentUserId : Guid.Empty;
        var result = await postManager.GetWallPostsAsync(userId, viewerId, request, ct);
        return ApiOkPaged(result);
    }

    [HttpGet("feed")]
    [Authorize]
    public async Task<IActionResult> GetFeed([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await postManager.GetFeedAsync(CurrentUserId, request, ct);
        return ApiOkPaged(result);
    }

    [HttpPost("{id:guid}/share")]
    [Authorize]
    public async Task<IActionResult> SharePost(Guid id, [FromBody] SharePostDto dto, CancellationToken ct)
    {
        dto.OriginalPostId = id;
        var result = await postManager.SharePostAsync(CurrentUserId, dto, ct);
        return ApiCreated(result, "Post shared successfully.");
    }

    [HttpPost("{id:guid}/report")]
    [Authorize]
    public async Task<IActionResult> ReportPost(Guid id, [FromBody] ReportPostDto dto, CancellationToken ct)
    {
        await postManager.ReportPostAsync(CurrentUserId, id, dto, ct);
        return ApiOk<object>(null!, "Post reported. Thank you.");
    }
}
