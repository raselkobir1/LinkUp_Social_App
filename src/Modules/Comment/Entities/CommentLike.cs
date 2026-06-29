using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.Comment.Entities;

public class CommentLike : BaseEntity
{
    public Guid CommentId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Comment Comment { get; set; } = null!;
}
