using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.Chat.Entities;

public class ChatParticipant : BaseEntity
{
    public Guid ChatId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool IsAdmin { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
