using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.Post.Entities;

public class PostImage : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid MediaFileId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int DisplayOrder { get; set; }

    public Post Post { get; set; } = null!;
}
