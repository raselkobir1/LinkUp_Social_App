using LinkUp.BuildingBlocks.Common.Entities;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.UserProfile.Entities;

public class SocialLink : AuditableEntity
{
    public Guid UserId { get; set; }
    public SocialPlatform Platform { get; set; }
    public string Url { get; set; } = string.Empty;
}
