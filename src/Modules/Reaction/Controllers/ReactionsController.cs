using Asp.Versioning;
using LinkUp.BuildingBlocks.Common.Controllers;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Reaction.DTOs;
using LinkUp.Modules.Reaction.Interfaces;
using LinkUp.SharedKernel.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Modules.Reaction.Controllers;

[ApiVersion("1.0")]
public class ReactionsController(IReactionManager reactionManager) : BaseApiController
{
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddOrUpdateReaction([FromBody] AddReactionDto dto, CancellationToken ct)
    {
        var result = await reactionManager.AddOrUpdateReactionAsync(CurrentUserId, dto, ct);
        return ApiCreated(result);
    }

    [HttpDelete("{targetType}/{targetId}")]
    [Authorize]
    public async Task<IActionResult> RemoveReaction(string targetType, Guid targetId, CancellationToken ct)
    {
        var result = await reactionManager.RemoveReactionAsync(CurrentUserId, targetType, targetId, ct);
        return ApiOk(result);
    }

    [HttpGet("{targetType}/{targetId}")]
    public async Task<IActionResult> GetReactionCounts(string targetType, Guid targetId, CancellationToken ct)
    {
        Guid? viewerId = IsAuthenticated ? (Guid?)CurrentUserId : null;
        var result = await reactionManager.GetReactionCountsAsync(targetType, targetId, viewerId, ct);
        return ApiOk(result);
    }

    [HttpGet("{targetType}/{targetId}/reactors")]
    public async Task<IActionResult> GetReactors(
        string targetType,
        Guid targetId,
        [FromQuery] ReactionType? filter,
        [FromQuery] PagedRequest request,
        CancellationToken ct)
    {
        var result = await reactionManager.GetReactorsAsync(targetType, targetId, filter, request, ct);
        return ApiOkPaged(result);
    }
}
