namespace LinkUp.Modules.Notification.DTOs;

public class NotificationSettingsDto
{
    public bool FriendRequests { get; set; } = true;
    public bool PostReactions { get; set; } = true;
    public bool Comments { get; set; } = true;
    public bool Mentions { get; set; } = true;
    public bool Messages { get; set; } = true;
    public bool GroupInvites { get; set; } = true;
    public bool VideoCalls { get; set; } = true;
}
