using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Post.DTOs;

public class CreatePostDto
{
    public string? Content { get; set; }
    public PostType PostType { get; set; }
    public PostVisibility Visibility { get; set; }
    public Guid? WallUserId { get; set; }
    public List<Guid> ImageIds { get; set; } = [];
    public List<string> ImageUrls { get; set; } = [];
    public Guid? VideoId { get; set; }
    public string? VideoUrl { get; set; }
}
