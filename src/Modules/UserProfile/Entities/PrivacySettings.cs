using LinkUp.BuildingBlocks.Common.Entities;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.UserProfile.Entities;

public class PrivacySettings : BaseEntity
{
    public Guid UserId { get; set; }
    public PostVisibility ProfileVisibility { get; set; } = PostVisibility.Public;
    public PostVisibility FriendListVisibility { get; set; } = PostVisibility.Friends;
    public PostVisibility PostDefaultVisibility { get; set; } = PostVisibility.Friends;
}
