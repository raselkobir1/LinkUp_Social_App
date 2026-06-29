using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Post.DTOs;

public class SharePostDto
{
    public Guid OriginalPostId { get; set; }
    public string? Content { get; set; }
    public PostVisibility Visibility { get; set; }
    public Guid? WallUserId { get; set; }
}
