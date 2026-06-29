using Asp.Versioning;
using LinkUp.BuildingBlocks.Common.Controllers;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Comment.DTOs;
using LinkUp.Modules.Comment.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Modules.Comment.Controllers;

[ApiVersion("1.0")]
public class CommentsController(ICommentManager commentManager) : BaseApiController
{
    // POST api/v1/comments
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddComment([FromBody] CreateCommentDto dto, CancellationToken ct)
    {
        var result = await commentManager.AddCommentAsync(CurrentUserId, dto, ct);
        return ApiCreated(result);
    }

    // PUT api/v1/comments/{id}
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateComment(Guid id, [FromBody] UpdateCommentDto dto, CancellationToken ct)
    {
        var result = await commentManager.UpdateCommentAsync(CurrentUserId, id, dto, ct);
        return ApiOk(result);
    }

    // DELETE api/v1/comments/{id}
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(Guid id, CancellationToken ct)
    {
        await commentManager.DeleteCommentAsync(CurrentUserId, id, ct);
        return ApiOk<object>(null!, "Comment deleted");
    }

    // POST api/v1/comments/{id}/like
    [HttpPost("{id:guid}/like")]
    [Authorize]
    public async Task<IActionResult> LikeComment(Guid id, CancellationToken ct)
    {
        await commentManager.LikeCommentAsync(CurrentUserId, id, ct);
        return ApiOk<object>(null!, "Liked");
    }

    // DELETE api/v1/comments/{id}/like
    [HttpDelete("{id:guid}/like")]
    [Authorize]
    public async Task<IActionResult> UnlikeComment(Guid id, CancellationToken ct)
    {
        await commentManager.UnlikeCommentAsync(CurrentUserId, id, ct);
        return ApiOk<object>(null!, "Unliked");
    }

    // GET api/v1/comments/post/{postId}
    [HttpGet("post/{postId:guid}")]
    public async Task<IActionResult> GetPostComments(Guid postId, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var viewerId = IsAuthenticated ? CurrentUserId : Guid.Empty;
        var result = await commentManager.GetPostCommentsAsync(postId, viewerId, request, ct);
        return ApiOkPaged(result);
    }

    // GET api/v1/comments/{id}/replies
    [HttpGet("{id:guid}/replies")]
    public async Task<IActionResult> GetReplies(Guid id, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var viewerId = IsAuthenticated ? CurrentUserId : Guid.Empty;
        var result = await commentManager.GetRepliesAsync(id, viewerId, request, ct);
        return ApiOkPaged(result);
    }
}
