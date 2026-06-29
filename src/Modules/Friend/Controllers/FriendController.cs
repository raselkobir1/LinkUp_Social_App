using Asp.Versioning;
using LinkUp.BuildingBlocks.Common.Controllers;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Friend.DTOs;
using LinkUp.Modules.Friend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Modules.Friend.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/friends")]
[Authorize]
public class FriendController(IFriendManager friendManager) : BaseApiController
{
    // POST api/v1/friends/request
    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto dto, CancellationToken ct)
    {
        await friendManager.SendRequestAsync(CurrentUserId, dto, ct);
        return ApiCreated<object>(null!, "Friend request sent.");
    }

    // PUT api/v1/friends/request/{id}/accept
    [HttpPut("request/{id:guid}/accept")]
    public async Task<IActionResult> AcceptRequest(Guid id, CancellationToken ct)
    {
        await friendManager.AcceptRequestAsync(id, CurrentUserId, ct);
        return ApiOk<object>(null!, "Friend request accepted.");
    }

    // PUT api/v1/friends/request/{id}/reject
    [HttpPut("request/{id:guid}/reject")]
    public async Task<IActionResult> RejectRequest(Guid id, CancellationToken ct)
    {
        await friendManager.RejectRequestAsync(id, CurrentUserId, ct);
        return ApiOk<object>(null!, "Friend request rejected.");
    }

    // DELETE api/v1/friends/request/{id}
    [HttpDelete("request/{id:guid}")]
    public async Task<IActionResult> CancelRequest(Guid id, CancellationToken ct)
    {
        await friendManager.CancelRequestAsync(id, CurrentUserId, ct);
        return ApiOk<object>(null!, "Friend request cancelled.");
    }

    // DELETE api/v1/friends/{friendId}
    [HttpDelete("{friendId:guid}")]
    public async Task<IActionResult> Unfriend(Guid friendId, CancellationToken ct)
    {
        await friendManager.UnfriendAsync(CurrentUserId, friendId, ct);
        return ApiOk<object>(null!, "Unfriended successfully.");
    }

    // GET api/v1/friends
    [HttpGet]
    public async Task<IActionResult> GetFriendList([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await friendManager.GetFriendListAsync(CurrentUserId, request, ct);
        return ApiOkPaged(result);
    }

    // GET api/v1/friends/requests/pending
    [HttpGet("requests/pending")]
    public async Task<IActionResult> GetPendingRequests([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await friendManager.GetPendingRequestsAsync(CurrentUserId, request, ct);
        return ApiOkPaged(result);
    }

    // GET api/v1/friends/requests/sent
    [HttpGet("requests/sent")]
    public async Task<IActionResult> GetSentRequests([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await friendManager.GetSentRequestsAsync(CurrentUserId, request, ct);
        return ApiOkPaged(result);
    }

    // GET api/v1/friends/mutual/{userId}
    [HttpGet("mutual/{userId:guid}")]
    public async Task<IActionResult> GetMutualFriends(Guid userId, CancellationToken ct)
    {
        var result = await friendManager.GetMutualFriendsAsync(CurrentUserId, userId, ct);
        return ApiOk(result);
    }

    // GET api/v1/friends/status/{userId}
    [HttpGet("status/{userId:guid}")]
    public async Task<IActionResult> GetFriendshipStatus(Guid userId, CancellationToken ct)
    {
        var result = await friendManager.GetFriendshipStatusAsync(CurrentUserId, userId, ct);
        return ApiOk(result);
    }

    // GET api/v1/friends/suggestions
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] int count = 10, CancellationToken ct = default)
    {
        var result = await friendManager.GetFriendSuggestionsAsync(CurrentUserId, count, ct);
        return ApiOk(result);
    }

    // POST api/v1/friends/block/{userId}
    [HttpPost("block/{userId:guid}")]
    public async Task<IActionResult> BlockUser(Guid userId, CancellationToken ct)
    {
        await friendManager.BlockUserAsync(CurrentUserId, userId, ct);
        return ApiOk<object>(null!, "User blocked.");
    }

    // DELETE api/v1/friends/block/{userId}
    [HttpDelete("block/{userId:guid}")]
    public async Task<IActionResult> UnblockUser(Guid userId, CancellationToken ct)
    {
        await friendManager.UnblockUserAsync(CurrentUserId, userId, ct);
        return ApiOk<object>(null!, "User unblocked.");
    }

    // GET api/v1/friends/blocked
    [HttpGet("blocked")]
    public async Task<IActionResult> GetBlocked(CancellationToken ct)
    {
        var result = await friendManager.GetBlockedUsersAsync(CurrentUserId, ct);
        return ApiOk(result);
    }
}
