using LinkUp.BuildingBlocks.Common.Entities;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Chat.Entities;

public class Message : AuditableEntity
{
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public string? Content { get; set; }
    public MessageType MessageType { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public Guid? ReplyToMessageId { get; set; }
    public bool IsDeletedForSender { get; set; } = false;
    public bool IsDeletedForEveryone { get; set; } = false;
    public DateTime? EditedAt { get; set; }
}
