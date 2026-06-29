namespace LinkUp.Modules.Comment.DTOs;

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public int LikeCount { get; set; }
    public int ReplyCount { get; set; }
    public CommentAuthorDto Author { get; set; } = null!;
    public bool IsLikedByCurrentUser { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<CommentDto>? Replies { get; set; }

    // Flat fields consumed by the Angular client (derived from Author).
    public Guid AuthorId => Author?.Id ?? Guid.Empty;
    public string AuthorName => Author?.FullName ?? string.Empty;
    public string? AuthorProfilePicture => Author?.ProfilePictureUrl;
    public bool IsLiked => IsLikedByCurrentUser;
}
