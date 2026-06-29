using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Notification.DTOs;

public record CreateNotificationDto(
    Guid RecipientId,
    Guid? SenderId,
    NotificationType Type,
    Guid? EntityId,
    string? EntityType,
    string Message);

public class SenderInfoDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid RecipientId { get; set; }
    public Guid? SenderId { get; set; }
    public NotificationType Type { get; set; }
    public Guid? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public SenderInfoDto? Sender { get; set; }

    // Flat fields consumed by the Angular client (derived from Sender).
    public string? SenderName => Sender?.FullName;
    public string? SenderProfilePicture => Sender?.ProfilePictureUrl;
}

public class NotificationCountDto
{
    public int UnreadCount { get; set; }
}
