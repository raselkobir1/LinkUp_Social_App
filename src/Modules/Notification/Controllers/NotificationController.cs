using Asp.Versioning;
using LinkUp.BuildingBlocks.Common.Controllers;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Notification.DTOs;
using LinkUp.Modules.Notification.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Modules.Notification.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
public class NotificationController(INotificationManager notificationManager) : BaseApiController
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetNotifications([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await notificationManager.GetNotificationsAsync(CurrentUserId, request, ct);
        return ApiOkPaged(result);
    }

    [HttpPut("{id:guid}/read")]
    [Authorize]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        await notificationManager.MarkAsReadAsync(id, CurrentUserId, ct);
        return ApiOk<object>(null!, "Notification marked as read.");
    }

    [HttpPut("read-all")]
    [Authorize]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        await notificationManager.MarkAllAsReadAsync(CurrentUserId, ct);
        return ApiOk<object>(null!, "All notifications marked as read.");
    }

    [HttpGet("unread-count")]
    [Authorize]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var count = await notificationManager.GetUnreadCountAsync(CurrentUserId, ct);
        return ApiOk(new NotificationCountDto { UnreadCount = count });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken ct)
    {
        await notificationManager.DeleteNotificationAsync(id, CurrentUserId, ct);
        return ApiOk<object>(null!, "Notification deleted.");
    }
}
