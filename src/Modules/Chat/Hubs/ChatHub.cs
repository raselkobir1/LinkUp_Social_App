using LinkUp.Modules.Chat.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LinkUp.Modules.Chat.Hubs;

[Authorize]
public class ChatHub(ChatDbContext db) : Hub
{
    private Guid CurrentUserId => Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUserId;

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

        // Broadcast offline status to other connected clients
        await Clients.Others.SendAsync("UserOffline", userId, DateTime.UtcNow);
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

    /// <summary>Relay a message-read event to other connected clients.
    /// Actual persistence is handled via the REST endpoint / ChatManager.</summary>
    public async Task MarkAsRead(string messageId)
    {
        var userId = CurrentUserId;
        await Clients.Others.SendAsync("MessageRead", Guid.Parse(messageId), userId);
    }
}
