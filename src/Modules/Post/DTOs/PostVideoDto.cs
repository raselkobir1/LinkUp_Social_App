namespace LinkUp.Modules.Post.DTOs;

public class PostVideoDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}
