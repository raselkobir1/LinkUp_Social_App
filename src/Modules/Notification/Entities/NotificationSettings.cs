using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.Notification.Entities;

/// <summary>Per-user toggles controlling which notification categories are generated.</summary>
public class NotificationSettings : BaseEntity
{
    public Guid UserId { get; set; }
    public bool FriendRequests { get; set; } = true;
    public bool PostReactions { get; set; } = true;
    public bool Comments { get; set; } = true;
    public bool Mentions { get; set; } = true;
    public bool Messages { get; set; } = true;
    public bool GroupInvites { get; set; } = true;
    public bool VideoCalls { get; set; } = true;
}
