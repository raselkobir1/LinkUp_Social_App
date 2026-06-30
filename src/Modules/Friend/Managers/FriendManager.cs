using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Friend.Configuration;
using LinkUp.Modules.Friend.DTOs;
using LinkUp.Modules.Friend.Entities;
using LinkUp.Modules.Friend.Interfaces;
using LinkUp.Modules.Identity.Entities;
using LinkUp.Modules.Notification.DTOs;
using LinkUp.Modules.Notification.Interfaces;
using LinkUp.SharedKernel.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.Friend.Managers;

public class FriendManager(
    FriendDbContext db,
    UserManager<ApplicationUser> userManager,
    INotificationManager notificationManager) : IFriendManager
{
    public async Task SendRequestAsync(Guid senderId, SendFriendRequestDto dto, CancellationToken ct = default)
    {
        if (senderId == dto.ReceiverId)
            throw new ValidationException("You cannot send a friend request to yourself.");

        if (await IsBlockedAsync(senderId, dto.ReceiverId, ct))
            throw new ForbiddenException("You cannot send a friend request to this user.");

        var already = await db.FriendRequests
            .AnyAsync(r =>
                ((r.SenderId == senderId && r.ReceiverId == dto.ReceiverId) ||
                 (r.SenderId == dto.ReceiverId && r.ReceiverId == senderId)) &&
                r.Status == FriendRequestStatus.Pending && !r.IsDeleted, ct);

        if (already)
            throw new ConflictException("A pending friend request already exists between these users.");

        var alreadyFriends = await db.Friendships
            .AnyAsync(f => f.UserId == senderId && f.FriendId == dto.ReceiverId, ct);

        if (alreadyFriends)
            throw new ConflictException("You are already friends with this user.");

        var sender = await userManager.FindByIdAsync(senderId.ToString())
            ?? throw new NotFoundException("User", senderId);

        var receiver = await userManager.FindByIdAsync(dto.ReceiverId.ToString())
            ?? throw new NotFoundException("User", dto.ReceiverId);

        var request = new FriendRequest
        {
            SenderId = senderId,
            SenderName = sender.FullName,
            SenderProfilePictureUrl = sender.ProfilePictureUrl,
            ReceiverId = dto.ReceiverId,
            ReceiverName = receiver.FullName,
            ReceiverProfilePictureUrl = receiver.ProfilePictureUrl,
            Status = FriendRequestStatus.Pending
        };

        db.FriendRequests.Add(request);
        await db.SaveChangesAsync(ct);

        await notificationManager.CreateNotificationAsync(new CreateNotificationDto(
            dto.ReceiverId, senderId, NotificationType.FriendRequest,
            request.Id, "FriendRequest", $"{sender.FullName} sent you a friend request"), ct);
    }

    public async Task AcceptRequestAsync(Guid requestId, Guid userId, CancellationToken ct = default)
    {
        var request = await db.FriendRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == userId
                && r.Status == FriendRequestStatus.Pending && !r.IsDeleted, ct)
            ?? throw new NotFoundException("FriendRequest", requestId);

        request.Status = FriendRequestStatus.Accepted;
        request.RespondedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        // Store as two rows for easy lookup
        db.Friendships.Add(new Friendship
        {
            UserId = request.SenderId,
            UserName = request.SenderName,
            UserProfilePictureUrl = request.SenderProfilePictureUrl,
            FriendId = request.ReceiverId,
            FriendName = request.ReceiverName,
            FriendProfilePictureUrl = request.ReceiverProfilePictureUrl
        });

        db.Friendships.Add(new Friendship
        {
            UserId = request.ReceiverId,
            UserName = request.ReceiverName,
            UserProfilePictureUrl = request.ReceiverProfilePictureUrl,
            FriendId = request.SenderId,
            FriendName = request.SenderName,
            FriendProfilePictureUrl = request.SenderProfilePictureUrl
        });

        await db.SaveChangesAsync(ct);

        // Notify the original requester that their request was accepted.
        await notificationManager.CreateNotificationAsync(new CreateNotificationDto(
            request.SenderId, request.ReceiverId, NotificationType.FriendRequestAccepted,
            request.Id, "FriendRequest", $"{request.ReceiverName} accepted your friend request"), ct);
    }

    public async Task RejectRequestAsync(Guid requestId, Guid userId, CancellationToken ct = default)
    {
        var request = await db.FriendRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == userId
                && r.Status == FriendRequestStatus.Pending && !r.IsDeleted, ct)
            ?? throw new NotFoundException("FriendRequest", requestId);

        request.Status = FriendRequestStatus.Rejected;
        request.RespondedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task CancelRequestAsync(Guid requestId, Guid userId, CancellationToken ct = default)
    {
        var request = await db.FriendRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.SenderId == userId
                && r.Status == FriendRequestStatus.Pending && !r.IsDeleted, ct)
            ?? throw new NotFoundException("FriendRequest", requestId);

        request.Status = FriendRequestStatus.Cancelled;
        request.IsDeleted = true;
        request.DeletedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task UnfriendAsync(Guid userId, Guid friendId, CancellationToken ct = default)
    {
        var friendships = await db.Friendships
            .Where(f =>
                (f.UserId == userId && f.FriendId == friendId) ||
                (f.UserId == friendId && f.FriendId == userId))
            .ToListAsync(ct);

        if (friendships.Count == 0)
            throw new NotFoundException("Friendship not found.");

        db.Friendships.RemoveRange(friendships);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<FriendDto>> GetFriendListAsync(Guid userId, PagedRequest request, CancellationToken ct = default)
    {
        var query = db.Friendships
            .AsNoTracking()
            .Where(f => f.UserId == userId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(f => new FriendDto
        {
            UserId = f.UserId,
            FriendInfo = new UserCardDto
            {
                Id = f.FriendId,
                FullName = f.FriendName,
                ProfilePictureUrl = f.FriendProfilePictureUrl
            },
            FriendSince = f.CreatedAt
        }).ToList();

        return PagedResult<FriendDto>.Create(dtos, total, request.PageNumber, request.PageSize);
    }

    public async Task<PagedResult<FriendRequestDto>> GetPendingRequestsAsync(Guid userId, PagedRequest request, CancellationToken ct = default)
    {
        var query = db.FriendRequests
            .AsNoTracking()
            .Where(r => r.ReceiverId == userId && r.Status == FriendRequestStatus.Pending && !r.IsDeleted);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToFriendRequestDto).ToList();

        return PagedResult<FriendRequestDto>.Create(dtos, total, request.PageNumber, request.PageSize);
    }

    public async Task<PagedResult<FriendRequestDto>> GetSentRequestsAsync(Guid userId, PagedRequest request, CancellationToken ct = default)
    {
        var query = db.FriendRequests
            .AsNoTracking()
            .Where(r => r.SenderId == userId && r.Status == FriendRequestStatus.Pending && !r.IsDeleted);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToFriendRequestDto).ToList();

        return PagedResult<FriendRequestDto>.Create(dtos, total, request.PageNumber, request.PageSize);
    }

    public async Task<List<UserCardDto>> GetMutualFriendsAsync(Guid userId, Guid otherUserId, CancellationToken ct = default)
    {
        var userFriendIds = await db.Friendships
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => f.FriendId)
            .ToListAsync(ct);

        var mutualFriends = await db.Friendships
            .AsNoTracking()
            .Where(f => f.UserId == otherUserId && userFriendIds.Contains(f.FriendId))
            .ToListAsync(ct);

        return mutualFriends.Select(f => new UserCardDto
        {
            Id = f.FriendId,
            FullName = f.FriendName,
            ProfilePictureUrl = f.FriendProfilePictureUrl
        }).ToList();
    }

    public async Task<FriendshipStatusDto> GetFriendshipStatusAsync(Guid userId, Guid otherUserId, CancellationToken ct = default)
    {
        var isFriend = await db.Friendships
            .AnyAsync(f => f.UserId == userId && f.FriendId == otherUserId, ct);

        if (isFriend)
            return new FriendshipStatusDto { Status = FriendshipStatus.Friends };

        var sentRequest = await db.FriendRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SenderId == userId && r.ReceiverId == otherUserId
                && r.Status == FriendRequestStatus.Pending && !r.IsDeleted, ct);

        if (sentRequest is not null)
            return new FriendshipStatusDto { Status = FriendshipStatus.Pending, RequestId = sentRequest.Id };

        var receivedRequest = await db.FriendRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SenderId == otherUserId && r.ReceiverId == userId
                && r.Status == FriendRequestStatus.Pending && !r.IsDeleted, ct);

        if (receivedRequest is not null)
            return new FriendshipStatusDto { Status = FriendshipStatus.RequestReceived, RequestId = receivedRequest.Id };

        return new FriendshipStatusDto { Status = FriendshipStatus.None };
    }

    public async Task<List<UserCardDto>> GetFriendSuggestionsAsync(Guid userId, int count, CancellationToken ct = default)
    {
        // Suggestions: friends-of-friends not yet friends with the user
        var myFriendIds = await db.Friendships
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => f.FriendId)
            .ToListAsync(ct);

        var pendingIds = await db.FriendRequests
            .AsNoTracking()
            .Where(r => (r.SenderId == userId || r.ReceiverId == userId)
                && r.Status == FriendRequestStatus.Pending && !r.IsDeleted)
            .Select(r => r.SenderId == userId ? r.ReceiverId : r.SenderId)
            .ToListAsync(ct);

        var excludedIds = myFriendIds.Concat(pendingIds).Append(userId).Distinct().ToList();

        var suggestions = await db.Friendships
            .AsNoTracking()
            .Where(f => myFriendIds.Contains(f.UserId) && !excludedIds.Contains(f.FriendId))
            .GroupBy(f => new { f.FriendId, f.FriendName, f.FriendProfilePictureUrl })
            .OrderByDescending(g => g.Count())
            .Take(count)
            .Select(g => new UserCardDto
            {
                Id = g.Key.FriendId,
                FullName = g.Key.FriendName,
                ProfilePictureUrl = g.Key.FriendProfilePictureUrl
            })
            .ToListAsync(ct);

        return suggestions;
    }

    public async Task<bool> IsFriendAsync(Guid userId, Guid otherUserId, CancellationToken ct = default)
    {
        return await db.Friendships
            .AnyAsync(f => f.UserId == userId && f.FriendId == otherUserId, ct);
    }

    private static FriendRequestDto MapToFriendRequestDto(FriendRequest r) => new()
    {
        Id = r.Id,
        Sender = new UserCardDto
        {
            Id = r.SenderId,
            FullName = r.SenderName,
            ProfilePictureUrl = r.SenderProfilePictureUrl
        },
        Receiver = new UserCardDto
        {
            Id = r.ReceiverId,
            FullName = r.ReceiverName,
            ProfilePictureUrl = r.ReceiverProfilePictureUrl
        },
        Status = r.Status,
        SentAt = r.CreatedAt,
        RespondedAt = r.RespondedAt
    };

    public async Task BlockUserAsync(Guid userId, Guid targetId, CancellationToken ct = default)
    {
        if (userId == targetId)
            throw new ValidationException("You cannot block yourself.");

        var target = await userManager.FindByIdAsync(targetId.ToString())
            ?? throw new NotFoundException("User", targetId);

        var existing = await db.BlockedUsers
            .AnyAsync(b => b.BlockerId == userId && b.BlockedId == targetId, ct);
        if (existing) return; // idempotent

        // Blocking severs any friendship and cancels pending requests between the two.
        var friendships = await db.Friendships
            .Where(f => (f.UserId == userId && f.FriendId == targetId) ||
                        (f.UserId == targetId && f.FriendId == userId))
            .ToListAsync(ct);
        db.Friendships.RemoveRange(friendships);

        var pending = await db.FriendRequests
            .Where(r => r.Status == FriendRequestStatus.Pending && !r.IsDeleted &&
                        ((r.SenderId == userId && r.ReceiverId == targetId) ||
                         (r.SenderId == targetId && r.ReceiverId == userId)))
            .ToListAsync(ct);
        foreach (var r in pending) r.Status = FriendRequestStatus.Rejected;

        db.BlockedUsers.Add(new BlockedUser
        {
            BlockerId = userId,
            BlockedId = targetId,
            BlockedName = target.FullName,
            BlockedProfilePictureUrl = target.ProfilePictureUrl
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task UnblockUserAsync(Guid userId, Guid targetId, CancellationToken ct = default)
    {
        var block = await db.BlockedUsers
            .FirstOrDefaultAsync(b => b.BlockerId == userId && b.BlockedId == targetId, ct)
            ?? throw new NotFoundException("Block record", targetId);

        db.BlockedUsers.Remove(block);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<UserCardDto>> GetBlockedUsersAsync(Guid userId, CancellationToken ct = default) =>
        await db.BlockedUsers
            .Where(b => b.BlockerId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new UserCardDto
            {
                Id = b.BlockedId,
                FullName = b.BlockedName,
                ProfilePictureUrl = b.BlockedProfilePictureUrl
            })
            .ToListAsync(ct);

    public async Task<bool> IsBlockedAsync(Guid userId, Guid otherUserId, CancellationToken ct = default) =>
        await db.BlockedUsers.AnyAsync(b =>
            (b.BlockerId == userId && b.BlockedId == otherUserId) ||
            (b.BlockerId == otherUserId && b.BlockedId == userId), ct);
}
