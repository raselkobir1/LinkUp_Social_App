using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Notification.DTOs;

namespace LinkUp.Modules.Notification.Interfaces;

public interface INotificationManager
{
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, CancellationToken ct = default);
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, PagedRequest request, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task DeleteNotificationAsync(Guid notificationId, Guid userId, CancellationToken ct = default);

    Task<NotificationSettingsDto> GetSettingsAsync(Guid userId, CancellationToken ct = default);
    Task<NotificationSettingsDto> UpdateSettingsAsync(Guid userId, NotificationSettingsDto dto, CancellationToken ct = default);
}
