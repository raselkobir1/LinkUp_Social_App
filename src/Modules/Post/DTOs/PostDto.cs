using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Post.DTOs;

public class PostDto
{
    public Guid Id { get; set; }
    public string? Content { get; set; }
    public PostType PostType { get; set; }
    public PostVisibility Visibility { get; set; }
    public bool IsPinned { get; set; }
    public int ShareCount { get; set; }
    public int CommentCount { get; set; }
    public int ReactionCount { get; set; }
    public PostAuthorDto Author { get; set; } = null!;
    public List<PostImageDto> Images { get; set; } = [];
    public PostVideoDto? Video { get; set; }
    public PostDto? OriginalPost { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
