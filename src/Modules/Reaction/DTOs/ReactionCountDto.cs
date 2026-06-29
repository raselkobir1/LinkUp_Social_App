using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Reaction.DTOs;

public class ReactionCountDto
{
    public Dictionary<ReactionType, int> Counts { get; set; } = new();
    public int TotalCount { get; set; }
    public ReactionType? UserReaction { get; set; }
}
