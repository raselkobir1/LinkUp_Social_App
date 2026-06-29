using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.UserProfile.Entities;

public class UserProfile : AuditableEntity
{
    public Guid UserId { get; set; }
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }
}
