namespace LinkUp.Modules.Comment.DTOs;

public class CreateCommentDto
{
    public Guid PostId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}
