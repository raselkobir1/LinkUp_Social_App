using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.Post.Entities;

public class PostReport : AuditableEntity
{
    public Guid PostId { get; set; }
    public Guid ReportedById { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsResolved { get; set; } = false;

    public Post? Post { get; set; }
}
