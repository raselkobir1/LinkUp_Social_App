using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Media.DTOs;

public class MediaUploadResultDto
{
    public Guid Id { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public MediaFileType FileType { get; set; }
    public string? Format { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? Duration { get; set; }
}

public class MediaFileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public MediaFileType FileType { get; set; }
    public string? Format { get; set; }
    public long SizeInBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? Duration { get; set; }
    public string Folder { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
