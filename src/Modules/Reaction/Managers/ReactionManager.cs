using AutoMapper;
using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.BuildingBlocks.Infrastructure.Extensions;
using LinkUp.Modules.Identity.Entities;
using LinkUp.Modules.Notification.DTOs;
using LinkUp.Modules.Notification.Interfaces;
using LinkUp.Modules.Post.Configuration;
using LinkUp.Modules.Reaction.Configuration;
using LinkUp.Modules.Reaction.DTOs;
using LinkUp.Modules.Reaction.Interfaces;
using LinkUp.SharedKernel.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.Reaction.Managers;

public class ReactionManager(
    ReactionDbContext db,
    UserManager<ApplicationUser> userManager,
    PostDbContext postDb,
    INotificationManager notificationManager,
    IMapper mapper) : IReactionManager
{
    private static ReactionCountDto BuildReactionCountDto(
        IEnumerable<Entities.Reaction> reactions,
        Guid? viewerId)
    {
        var list = reactions.ToList();

        var counts = list
            .GroupBy(r => r.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        var totalCount = list.Count;

        ReactionType? userReaction = viewerId.HasValue
            ? list.FirstOrDefault(r => r.UserId == viewerId.Value)?.Type
            : null;

        return new ReactionCountDto
        {
            Counts = counts,
            TotalCount = totalCount,
            UserReaction = userReaction
        };
    }

    public async Task<ReactionCountDto> AddOrUpdateReactionAsync(
        Guid userId,
        AddReactionDto dto,
        CancellationToken ct = default)
    {
        var existing = await db.Reactions
            .FirstOrDefaultAsync(r =>
                r.UserId == userId &&
                r.TargetId == dto.TargetId &&
                r.TargetType == dto.TargetType, ct);

        if (existing is not null)
        {
            existing.Type = dto.Type;
        }
        else
        {
            var reaction = new Entities.Reaction
            {
                UserId = userId,
                TargetId = dto.TargetId,
                TargetType = dto.TargetType,
                Type = dto.Type
            };

            db.Reactions.Add(reaction);

            // Notify the post author when someone newly reacts to their post.
            if (string.Equals(dto.TargetType, "Post", StringComparison.OrdinalIgnoreCase))
            {
                var authorId = await postDb.Posts
                    .Where(p => p.Id == dto.TargetId && !p.IsDeleted)
                    .Select(p => (Guid?)p.AuthorId)
                    .FirstOrDefaultAsync(ct);
                if (authorId is { } aid && aid != userId)
                {
                    var actor = await userManager.FindByIdAsync(userId.ToString());
                    await db.SaveChangesAsync(ct);
                    await notificationManager.CreateNotificationAsync(new CreateNotificationDto(
                        aid, userId, NotificationType.PostLike,
                        dto.TargetId, "Post", $"{actor?.FullName ?? "Someone"} reacted to your post"), ct);
                    await SyncTargetReactionCountAsync(dto.TargetType, dto.TargetId, ct);
                    return await GetReactionCountsAsync(dto.TargetType, dto.TargetId, userId, ct);
                }
            }
        }

        await db.SaveChangesAsync(ct);
        await SyncTargetReactionCountAsync(dto.TargetType, dto.TargetId, ct);

        return await GetReactionCountsAsync(dto.TargetType, dto.TargetId, userId, ct);
    }

    /// <summary>
    /// Keeps the denormalized Post.ReactionCount in sync with the actual number of
    /// reactions, so the feed shows correct like counts. Recomputed rather than
    /// incremented to stay correct across add/update/remove and to self-heal.
    /// </summary>
    private async Task SyncTargetReactionCountAsync(string targetType, Guid targetId, CancellationToken ct)
    {
        if (!string.Equals(targetType, "Post", StringComparison.OrdinalIgnoreCase))
            return;

        var count = await db.Reactions
            .CountAsync(r => r.TargetId == targetId && r.TargetType == targetType, ct);

        await postDb.Posts
            .Where(p => p.Id == targetId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ReactionCount, count), ct);
    }

    public async Task<ReactionCountDto> RemoveReactionAsync(
        Guid userId,
        string targetType,
        Guid targetId,
        CancellationToken ct = default)
    {
        var reaction = await db.Reactions
            .FirstOrDefaultAsync(r =>
                r.UserId == userId &&
                r.TargetId == targetId &&
                r.TargetType == targetType, ct)
            ?? throw new NotFoundException("Reaction not found.");

        db.Reactions.Remove(reaction);
        await db.SaveChangesAsync(ct);
        await SyncTargetReactionCountAsync(targetType, targetId, ct);

        return await GetReactionCountsAsync(targetType, targetId, userId, ct);
    }

    public async Task<ReactionCountDto> GetReactionCountsAsync(
        string targetType,
        Guid targetId,
        Guid? viewerId,
        CancellationToken ct = default)
    {
        var reactions = await db.Reactions
            .AsNoTracking()
            .Where(r => r.TargetType == targetType && r.TargetId == targetId)
            .ToListAsync(ct);

        return BuildReactionCountDto(reactions, viewerId);
    }

    public async Task<PagedResult<ReactorDto>> GetReactorsAsync(
        string targetType,
        Guid targetId,
        ReactionType? filter,
        PagedRequest request,
        CancellationToken ct = default)
    {
        var query = db.Reactions
            .AsNoTracking()
            .Where(r => r.TargetType == targetType && r.TargetId == targetId);

        if (filter.HasValue)
            query = query.Where(r => r.Type == filter.Value);

        query = query.OrderByDescending(r => r.CreatedAt);

        var pagedReactions = await query.ToPagedResultAsync(request, ct);

        var reactorDtos = new List<ReactorDto>();

        foreach (var reaction in pagedReactions.Items)
        {
            var user = await userManager.FindByIdAsync(reaction.UserId.ToString());

            reactorDtos.Add(new ReactorDto
            {
                UserId = reaction.UserId,
                FullName = user?.FullName ?? string.Empty,
                ProfilePictureUrl = user?.ProfilePictureUrl,
                ReactionType = reaction.Type
            });
        }

        return PagedResult<ReactorDto>.Create(
            reactorDtos,
            pagedReactions.TotalCount,
            pagedReactions.PageNumber,
            pagedReactions.PageSize);
    }
}
