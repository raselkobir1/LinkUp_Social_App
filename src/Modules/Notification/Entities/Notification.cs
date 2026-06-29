using LinkUp.BuildingBlocks.Common.Entities;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Notification.Entities;

public class Notification : AuditableEntity
{
    public Guid RecipientId { get; set; }
    public Guid? SenderId { get; set; }
    public NotificationType Type { get; set; }
    public Guid? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}
