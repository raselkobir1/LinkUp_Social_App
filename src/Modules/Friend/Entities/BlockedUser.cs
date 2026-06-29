using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.Friend.Entities;

/// <summary>Records that <see cref="BlockerId"/> has blocked <see cref="BlockedId"/>.</summary>
public class BlockedUser : BaseEntity
{
    public Guid BlockerId { get; set; }
    public Guid BlockedId { get; set; }
    public string BlockedName { get; set; } = string.Empty;
    public string? BlockedProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
