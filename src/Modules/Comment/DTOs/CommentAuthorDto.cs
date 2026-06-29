namespace LinkUp.Modules.Comment.DTOs;

public class CommentAuthorDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string UserName { get; set; } = string.Empty;
}
