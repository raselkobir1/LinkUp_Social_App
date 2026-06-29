using Asp.Versioning;
using LinkUp.BuildingBlocks.Common.Controllers;
using LinkUp.Modules.Chat.DTOs;
using LinkUp.Modules.Chat.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Modules.Chat.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/groups")]
public class GroupChatController(IGroupChatManager groupChatManager) : BaseApiController
{
    /// <summary>Create a new group chat.</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateGroup(
        [FromBody] CreateGroupChatDto dto, CancellationToken ct)
    {
        var result = await groupChatManager.CreateGroupAsync(CurrentUserId, dto, ct);
        return ApiCreated(result, "Group chat created.");
    }

    /// <summary>Get details about a group chat (members, metadata).</summary>
    [HttpGet("{chatId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetGroupInfo(Guid chatId, CancellationToken ct)
    {
        var result = await groupChatManager.GetGroupInfoAsync(chatId, CurrentUserId, ct);
        return ApiOk(result);
    }

    /// <summary>Update a group chat's name and description (admin only).</summary>
    [HttpPut("{chatId:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateGroup(
        Guid chatId, [FromBody] UpdateGroupChatDto dto, CancellationToken ct)
    {
        var result = await groupChatManager.UpdateGroupAsync(CurrentUserId, chatId, dto, ct);
        return ApiOk(result, "Group updated.");
    }

    /// <summary>Add new members to a group chat (admin only).</summary>
    [HttpPost("{chatId:guid}/members")]
    [Authorize]
    public async Task<IActionResult> AddMembers(
        Guid chatId, [FromBody] AddGroupMembersDto dto, CancellationToken ct)
    {
        await groupChatManager.AddMembersAsync(CurrentUserId, chatId, dto, ct);
        return ApiOk<object>(null!, "Members added.");
    }

    /// <summary>Remove a member from a group chat (admin only, or self-removal).</summary>
    [HttpDelete("{chatId:guid}/members/{memberId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveMember(
        Guid chatId, Guid memberId, CancellationToken ct)
    {
        await groupChatManager.RemoveMemberAsync(CurrentUserId, chatId, memberId, ct);
        return ApiOk<object>(null!, "Member removed.");
    }

    /// <summary>Promote a participant to admin (admin only).</summary>
    [HttpPost("{chatId:guid}/members/{memberId:guid}/make-admin")]
    [Authorize]
    public async Task<IActionResult> AssignAdmin(
        Guid chatId, Guid memberId, CancellationToken ct)
    {
        await groupChatManager.AssignAdminAsync(CurrentUserId, chatId, memberId, ct);
        return ApiOk<object>(null!, "Admin assigned.");
    }

    /// <summary>Leave a group chat.</summary>
    [HttpPost("{chatId:guid}/leave")]
    [Authorize]
    public async Task<IActionResult> LeaveGroup(Guid chatId, CancellationToken ct)
    {
        await groupChatManager.LeaveGroupAsync(CurrentUserId, chatId, ct);
        return ApiOk<object>(null!, "You have left the group.");
    }

    /// <summary>Update the group photo URL (admin only).</summary>
    [HttpPut("{chatId:guid}/photo")]
    [Authorize]
    public async Task<IActionResult> ChangeGroupPhoto(
        Guid chatId, [FromBody] string photoUrl, CancellationToken ct)
    {
        await groupChatManager.ChangeGroupPhotoAsync(CurrentUserId, chatId, photoUrl, ct);
        return ApiOk<object>(null!, "Group photo updated.");
    }
}
