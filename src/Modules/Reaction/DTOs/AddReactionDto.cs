using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Reaction.DTOs;

public class AddReactionDto
{
    public Guid TargetId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public ReactionType Type { get; set; }
}
