using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.Chat.Entities;

public class Chat : AuditableEntity
{
    public bool IsGroup { get; set; } = false;
    public DateTime? LastMessageAt { get; set; }
}
