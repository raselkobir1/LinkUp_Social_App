using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.Comment.Entities;

public class Comment : AuditableEntity
{
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public int LikeCount { get; set; } = 0;
    public int ReplyCount { get; set; } = 0;

    public ICollection<CommentLike> Likes { get; set; } = [];
}
