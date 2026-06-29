using LinkUp.BuildingBlocks.Common.Entities;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Reaction.Entities;

public class Reaction : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid TargetId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public ReactionType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
