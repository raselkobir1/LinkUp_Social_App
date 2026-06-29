using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.Friend.Entities;

public class Friendship : BaseEntity
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserProfilePictureUrl { get; set; }

    public Guid FriendId { get; set; }
    public string FriendName { get; set; } = string.Empty;
    public string? FriendProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
