using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.UserProfile.DTOs;

public class SocialLinkDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public SocialPlatform Platform { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class CreateSocialLinkDto
{
    public SocialPlatform Platform { get; set; }
    public string Url { get; set; } = string.Empty;
}
