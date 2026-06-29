using AutoMapper;
using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.BuildingBlocks.Infrastructure.Extensions;
using LinkUp.Modules.Identity.Entities;
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
        }

        await db.SaveChangesAsync(ct);

        return await GetReactionCountsAsync(dto.TargetType, dto.TargetId, userId, ct);
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
