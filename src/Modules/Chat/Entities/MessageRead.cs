using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.Chat.Entities;

public class MessageRead : BaseEntity
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}
