using LinkUp.Modules.Chat.Configuration;
using LinkUp.Modules.Chat.Interfaces;
using LinkUp.Modules.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LinkUp.Modules.Chat.Hubs;

[Authorize]
public class ChatHub(ChatDbContext db, UserManager<ApplicationUser> userManager, IChatManager chatManager) : Hub
{
    private Guid CurrentUserId => Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUserId;

        // Mark the user online and broadcast presence to everyone else.
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
        {
            user.IsOnline = true;
            user.LastSeen = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
        }
        await Clients.Others.SendAsync("UserOnline", userId);

        // Join personal group so targeted notifications can be sent directly to this user
        await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());

        // Join all active chat groups so the user receives messages in real time
        var chatIds = await db.ChatParticipants
            .Where(cp => cp.UserId == userId && cp.IsActive)
            .Select(cp => cp.ChatId)
            .ToListAsync();

        foreach (var chatId in chatIds)
            await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = CurrentUserId;
        var now = DateTime.UtcNow;

        // Persist offline status + last-seen timestamp, then broadcast it.
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
        {
            user.IsOnline = false;
            user.LastSeen = now;
            await userManager.UpdateAsync(user);
        }
        await Clients.Others.SendAsync("UserOffline", userId, now);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Explicitly join a chat group (e.g., after creating a new chat).</summary>
    public async Task JoinChat(string chatId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId);

    /// <summary>Leave a chat group (e.g., after leaving a group chat).</summary>
    public async Task LeaveChat(string chatId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);

    /// <summary>Broadcast typing indicator to other members of the chat.</summary>
    public async Task SendTypingIndicator(string chatId, bool isTyping)
    {
        var userId = CurrentUserId;
        await Clients.OthersInGroup(chatId).SendAsync("UserTyping", Guid.Parse(chatId), userId, isTyping);
    }

    /// <summary>Recipient acknowledges a message reached their device. Persists the
    /// Delivered status and notifies the chat so the sender sees the ✓✓.</summary>
    public async Task MarkDelivered(string messageId, string chatId)
    {
        await chatManager.MarkDeliveredAsync(Guid.Parse(messageId), CurrentUserId);
        await Clients.OthersInGroup(chatId).SendAsync("MessageDelivered", Guid.Parse(messageId), CurrentUserId);
    }

    /// <summary>Persist + relay a message-read event to other connected clients.</summary>
    public async Task MarkAsRead(string messageId, string chatId)
    {
        var userId = CurrentUserId;
        await chatManager.MarkReadAsync(Guid.Parse(messageId), userId);
        await Clients.OthersInGroup(chatId).SendAsync("MessageRead", Guid.Parse(messageId), userId);
    }
}
