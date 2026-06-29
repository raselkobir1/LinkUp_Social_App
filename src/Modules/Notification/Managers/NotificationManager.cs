using AutoMapper;
using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Identity.Entities;
using LinkUp.Modules.Notification.Configuration;
using LinkUp.Modules.Notification.DTOs;
using LinkUp.Modules.Notification.Hubs;
using LinkUp.Modules.Notification.Interfaces;
using LinkUp.SharedKernel.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NotificationEntity = LinkUp.Modules.Notification.Entities.Notification;

namespace LinkUp.Modules.Notification.Managers;

public class NotificationManager(
    NotificationDbContext db,
    UserManager<ApplicationUser> userManager,
    IHubContext<NotificationHub> hubContext,
    IMapper mapper) : INotificationManager
{
    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, CancellationToken ct = default)
    {
        // Respect the recipient's notification settings — suppress disabled categories.
        if (!await IsTypeEnabledAsync(dto.RecipientId, dto.Type, ct))
            return new NotificationDto { Type = dto.Type, Message = dto.Message };

        var notification = new NotificationEntity
        {
            RecipientId = dto.RecipientId,
            SenderId = dto.SenderId,
            Type = dto.Type,
            EntityId = dto.EntityId,
            EntityType = dto.EntityType,
            Message = dto.Message,
            IsRead = false
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);

        var notificationDto = mapper.Map<NotificationDto>(notification);

        if (dto.SenderId.HasValue)
        {
            var sender = await userManager.FindByIdAsync(dto.SenderId.Value.ToString());
            if (sender is not null)
            {
                notificationDto.Sender = new SenderInfoDto
                {
                    Id = sender.Id,
                    FullName = sender.FullName,
                    ProfilePictureUrl = sender.ProfilePictureUrl
                };
            }
        }

        var recipientGroup = dto.RecipientId.ToString();

        await hubContext.Clients.Group(recipientGroup)
            .SendAsync("ReceiveNotification", notificationDto, ct);

        var unreadCount = await GetUnreadCountAsync(dto.RecipientId, ct);
        await hubContext.Clients.Group(recipientGroup)
            .SendAsync("NotificationCountUpdated", unreadCount, ct);

        return notificationDto;
    }

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, PagedRequest request, CancellationToken ct = default)
    {
        var query = db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == userId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var notifications = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = new List<NotificationDto>(notifications.Count);

        foreach (var notification in notifications)
        {
            var notificationDto = mapper.Map<NotificationDto>(notification);

            if (notification.SenderId.HasValue)
            {
                var sender = await userManager.FindByIdAsync(notification.SenderId.Value.ToString());
                if (sender is not null)
                {
                    notificationDto.Sender = new SenderInfoDto
                    {
                        Id = sender.Id,
                        FullName = sender.FullName,
                        ProfilePictureUrl = sender.ProfilePictureUrl
                    };
                }
            }

            dtos.Add(notificationDto);
        }

        return PagedResult<NotificationDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId && !n.IsDeleted, ct)
            ?? throw new NotFoundException("Notification", notificationId);

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        await db.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead && !n.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now)
                .SetProperty(n => n.UpdatedAt, now), ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.Notifications
            .CountAsync(n => n.RecipientId == userId && !n.IsRead && !n.IsDeleted, ct);
    }

    public async Task DeleteNotificationAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && !n.IsDeleted, ct)
            ?? throw new NotFoundException("Notification", notificationId);

        if (notification.RecipientId != userId)
            throw new ForbiddenException("You do not have permission to delete this notification.");

        notification.IsDeleted = true;
        notification.DeletedAt = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task<NotificationSettingsDto> GetSettingsAsync(Guid userId, CancellationToken ct = default)
    {
        var s = await GetOrCreateSettingsAsync(userId, ct);
        return new NotificationSettingsDto
        {
            FriendRequests = s.FriendRequests,
            PostReactions = s.PostReactions,
            Comments = s.Comments,
            Mentions = s.Mentions,
            Messages = s.Messages,
            GroupInvites = s.GroupInvites,
            VideoCalls = s.VideoCalls
        };
    }

    public async Task<NotificationSettingsDto> UpdateSettingsAsync(Guid userId, NotificationSettingsDto dto, CancellationToken ct = default)
    {
        var s = await GetOrCreateSettingsAsync(userId, ct);
        s.FriendRequests = dto.FriendRequests;
        s.PostReactions = dto.PostReactions;
        s.Comments = dto.Comments;
        s.Mentions = dto.Mentions;
        s.Messages = dto.Messages;
        s.GroupInvites = dto.GroupInvites;
        s.VideoCalls = dto.VideoCalls;
        await db.SaveChangesAsync(ct);
        return dto;
    }

    private async Task<Entities.NotificationSettings> GetOrCreateSettingsAsync(Guid userId, CancellationToken ct)
    {
        var s = await db.NotificationSettings.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (s is null)
        {
            s = new Entities.NotificationSettings { UserId = userId };
            db.NotificationSettings.Add(s);
            await db.SaveChangesAsync(ct);
        }
        return s;
    }

    private async Task<bool> IsTypeEnabledAsync(Guid userId, NotificationType type, CancellationToken ct)
    {
        var s = await db.NotificationSettings.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (s is null) return true; // default: all enabled
        return type switch
        {
            NotificationType.FriendRequest or NotificationType.FriendRequestAccepted => s.FriendRequests,
            NotificationType.PostLike => s.PostReactions,
            NotificationType.PostComment or NotificationType.CommentReply => s.Comments,
            NotificationType.Mention => s.Mentions,
            NotificationType.NewMessage => s.Messages,
            NotificationType.GroupInvite => s.GroupInvites,
            NotificationType.VideoCall => s.VideoCalls,
            _ => true
        };
    }
}
