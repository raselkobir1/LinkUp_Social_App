using LinkUp.BuildingBlocks.Common.Entities;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Post.Entities;

public class Post : AuditableEntity
{
    public Guid AuthorId { get; set; }
    public string? Content { get; set; }
    public PostType PostType { get; set; }
    public PostVisibility Visibility { get; set; }
    public bool IsPinned { get; set; } = false;
    public int ShareCount { get; set; } = 0;
    public Guid? OriginalPostId { get; set; }
    public Guid? WallUserId { get; set; }
    public int CommentCount { get; set; } = 0;
    public int ReactionCount { get; set; } = 0;

    public ICollection<PostImage> Images { get; set; } = [];
    public PostVideo? Video { get; set; }
    public Post? OriginalPost { get; set; }
}
