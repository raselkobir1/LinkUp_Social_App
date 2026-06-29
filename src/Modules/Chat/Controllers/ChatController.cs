using Asp.Versioning;
using LinkUp.BuildingBlocks.Common.Controllers;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Chat.DTOs;
using LinkUp.Modules.Chat.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Modules.Chat.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/chats")]
public class ChatController(IChatManager chatManager) : BaseApiController
{
    /// <summary>Create or retrieve an existing direct (1-to-1) chat with another user.</summary>
    [HttpPost("direct")]
    [Authorize]
    public async Task<IActionResult> CreateDirectChat(
        [FromBody] CreateDirectChatDto dto, CancellationToken ct)
    {
        var result = await chatManager.GetOrCreateDirectChatAsync(CurrentUserId, dto.TargetUserId, ct);
        return ApiCreated(result, "Chat created or retrieved.");
    }

    /// <summary>Get the list of all chats for the current user, ordered by most recent activity.</summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetChatList(CancellationToken ct)
    {
        var result = await chatManager.GetChatListAsync(CurrentUserId, ct);
        return ApiOk(result);
    }

    /// <summary>Get paginated messages for a given chat.</summary>
    [HttpGet("{chatId:guid}/messages")]
    [Authorize]
    public async Task<IActionResult> GetMessages(
        Guid chatId, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await chatManager.GetMessagesAsync(chatId, CurrentUserId, request, ct);
        return ApiOkPaged(result);
    }

    /// <summary>Send a new message to a chat.</summary>
    [HttpPost("messages")]
    [Authorize]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendMessageDto dto, CancellationToken ct)
    {
        var result = await chatManager.SendMessageAsync(CurrentUserId, dto, ct);
        return ApiCreated(result, "Message sent.");
    }

    /// <summary>Edit the content of an existing message (sender only).</summary>
    [HttpPut("messages/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> EditMessage(
        Guid id, [FromBody] UpdateMessageDto dto, CancellationToken ct)
    {
        var result = await chatManager.EditMessageAsync(CurrentUserId, id, dto, ct);
        return ApiOk(result, "Message updated.");
    }

    /// <summary>Delete a message only for the current user (soft delete).</summary>
    [HttpDelete("messages/{id:guid}/me")]
    [Authorize]
    public async Task<IActionResult> DeleteForMe(Guid id, CancellationToken ct)
    {
        await chatManager.DeleteForMeAsync(CurrentUserId, id, ct);
        return ApiOk<object>(null!, "Message deleted for you.");
    }

    /// <summary>Delete a message for all participants (sender only).</summary>
    [HttpDelete("messages/{id:guid}/everyone")]
    [Authorize]
    public async Task<IActionResult> DeleteForEveryone(Guid id, CancellationToken ct)
    {
        await chatManager.DeleteForEveryoneAsync(CurrentUserId, id, ct);
        return ApiOk<object>(null!, "Message deleted for everyone.");
    }

    /// <summary>Mark a message as read by the current user.</summary>
    [HttpPost("messages/{id:guid}/mark-read")]
    [Authorize]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await chatManager.MarkReadAsync(id, CurrentUserId, ct);
        return ApiOk<object>(null!, "Message marked as read.");
    }

    /// <summary>Search messages within a chat by content keyword.</summary>
    [HttpGet("{chatId:guid}/messages/search")]
    [Authorize]
    public async Task<IActionResult> SearchMessages(
        Guid chatId,
        [FromQuery] string query,
        [FromQuery] PagedRequest request,
        CancellationToken ct)
    {
        var result = await chatManager.SearchMessagesAsync(chatId, query, request, ct);
        return ApiOkPaged(result);
    }
}
