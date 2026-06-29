using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.UserProfile.DTOs;

public class PrivacySettingsDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public PostVisibility ProfileVisibility { get; set; }
    public PostVisibility FriendListVisibility { get; set; }
    public PostVisibility PostDefaultVisibility { get; set; }
}

public class UpdatePrivacySettingsDto
{
    public PostVisibility ProfileVisibility { get; set; }
    public PostVisibility FriendListVisibility { get; set; }
    public PostVisibility PostDefaultVisibility { get; set; }
}
