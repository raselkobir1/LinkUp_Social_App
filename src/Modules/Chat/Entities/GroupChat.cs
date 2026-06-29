using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.Chat.Entities;

public class GroupChat : AuditableEntity
{
    public Guid ChatId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GroupPhotoUrl { get; set; }
}
