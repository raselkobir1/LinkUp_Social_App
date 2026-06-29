using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.Chat.Entities;

public class MessageAttachment : BaseEntity
{
    public Guid MessageId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long FileSizeBytes { get; set; }
}
