namespace LinkUp.Modules.Post.DTOs;

public class PostImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int DisplayOrder { get; set; }
}
