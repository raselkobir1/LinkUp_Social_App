using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Reaction.DTOs;

public class ReactorDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public ReactionType ReactionType { get; set; }
}
