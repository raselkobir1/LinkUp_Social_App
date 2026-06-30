using AutoMapper;
using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.BuildingBlocks.Infrastructure.Extensions;
using LinkUp.Modules.Friend.Configuration;
using LinkUp.Modules.Identity.Entities;
using LinkUp.Modules.Post.Configuration;
using LinkUp.Modules.Post.DTOs;
using LinkUp.Modules.Post.Entities;
using LinkUp.Modules.Post.Interfaces;
using PostEntity = LinkUp.Modules.Post.Entities.Post;
using LinkUp.SharedKernel.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.Post.Managers;

public class PostManager(
    PostDbContext db,
    UserManager<ApplicationUser> userManager,
    FriendDbContext friendDb,
    IMapper mapper) : IPostManager
{
    public async Task<PostDto> CreatePostAsync(Guid userId, CreatePostDto dto, CancellationToken ct = default)
    {
        var post = new PostEntity
        {
            AuthorId = userId,
            Content = dto.Content,
            PostType = dto.PostType,
            Visibility = dto.Visibility,
            WallUserId = dto.WallUserId,
            CreatedById = userId
        };

        if (dto.ImageUrls.Count > 0)
        {
            for (var i = 0; i < dto.ImageUrls.Count; i++)
            {
                var imageId = i < dto.ImageIds.Count ? dto.ImageIds[i] : Guid.NewGuid();
                post.Images.Add(new PostImage
                {
                    MediaFileId = imageId,
                    Url = dto.ImageUrls[i],
                    DisplayOrder = i
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.VideoUrl))
        {
            post.Video = new PostVideo
            {
                MediaFileId = dto.VideoId ?? Guid.NewGuid(),
                Url = dto.VideoUrl
            };
        }

        db.Posts.Add(post);
        await db.SaveChangesAsync(ct);

        return await MapToPostDtoAsync(post, ct);
    }

    public async Task<PostDto> UpdatePostAsync(Guid userId, Guid postId, UpdatePostDto dto, CancellationToken ct = default)
    {
        var post = await db.Posts
            .Include(p => p.Images)
            .Include(p => p.Video)
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Post", postId);

        if (post.AuthorId != userId)
            throw new ForbiddenException("You are not allowed to edit this post.");

        post.Content = dto.Content;
        post.Visibility = dto.Visibility;
        post.UpdatedAt = DateTime.UtcNow;
        post.UpdatedById = userId;

        await db.SaveChangesAsync(ct);

        return await MapToPostDtoAsync(post, ct);
    }

    public async Task DeletePostAsync(Guid userId, Guid postId, CancellationToken ct = default)
    {
        var post = await db.Posts
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Post", postId);

        if (post.AuthorId != userId)
            throw new ForbiddenException("You are not allowed to delete this post.");

        post.IsDeleted = true;
        post.DeletedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        post.UpdatedById = userId;

        await db.SaveChangesAsync(ct);
    }

    public async Task<PostDto> PinPostAsync(Guid userId, Guid postId, bool pin, CancellationToken ct = default)
    {
        var post = await db.Posts
            .Include(p => p.Images)
            .Include(p => p.Video)
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Post", postId);

        if (post.AuthorId != userId)
            throw new ForbiddenException("You are not allowed to pin this post.");

        post.IsPinned = pin;
        post.UpdatedAt = DateTime.UtcNow;
        post.UpdatedById = userId;

        await db.SaveChangesAsync(ct);

        return await MapToPostDtoAsync(post, ct);
    }

    public async Task<PostDto> GetPostByIdAsync(Guid postId, Guid viewerId, CancellationToken ct = default)
    {
        var post = await db.Posts
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.Video)
            .Include(p => p.OriginalPost)
                .ThenInclude(op => op!.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.OriginalPost)
                .ThenInclude(op => op!.Video)
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Post", postId);

        if (post.Visibility == PostVisibility.OnlyMe && post.AuthorId != viewerId)
            throw new ForbiddenException("You do not have permission to view this post.");

        return await MapToPostDtoAsync(post, ct);
    }

    public async Task<PagedResult<PostDto>> GetWallPostsAsync(
        Guid wallUserId,
        Guid viewerId,
        PagedRequest request,
        CancellationToken ct = default)
    {
        var isOwner = viewerId == wallUserId;

        var query = db.Posts
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.Video)
            .Where(p => !p.IsDeleted &&
                        (p.WallUserId == wallUserId ||
                         (p.WallUserId == null && p.AuthorId == wallUserId)));

        if (!isOwner)
        {
            query = query.Where(p => p.Visibility == PostVisibility.Public);
        }

        query = query
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.CreatedAt);

        var pagedResult = await query.ToPagedResultAsync(request, ct);

        var mappedItems = new List<PostDto>();
        foreach (var post in pagedResult.Items)
        {
            mappedItems.Add(await MapToPostDtoAsync(post, ct));
        }

        return PagedResult<PostDto>.Create(mappedItems, pagedResult.TotalCount, pagedResult.PageNumber, pagedResult.PageSize);
    }

    public async Task<PagedResult<PostDto>> GetFeedAsync(
        Guid userId,
        PagedRequest request,
        CancellationToken ct = default)
    {
        var friendIds = await friendDb.Friendships
            .Where(f => f.UserId == userId || f.FriendId == userId)
            .Select(f => f.UserId == userId ? f.FriendId : f.UserId)
            .ToListAsync(ct);

        var query = db.Posts
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.Video)
            .Where(p => !p.IsDeleted &&
                        (p.AuthorId == userId || friendIds.Contains(p.AuthorId)) &&
                        (p.AuthorId == userId ||
                         p.Visibility == PostVisibility.Public ||
                         p.Visibility == PostVisibility.Friends))
            .OrderByDescending(p => p.CreatedAt);

        var pagedResult = await query.ToPagedResultAsync(request, ct);

        var mappedItems = new List<PostDto>();
        foreach (var post in pagedResult.Items)
        {
            mappedItems.Add(await MapToPostDtoAsync(post, ct));
        }

        return PagedResult<PostDto>.Create(mappedItems, pagedResult.TotalCount, pagedResult.PageNumber, pagedResult.PageSize);
    }

    public async Task<PostDto> SharePostAsync(Guid userId, SharePostDto dto, CancellationToken ct = default)
    {
        var originalPost = await db.Posts
            .FirstOrDefaultAsync(p => p.Id == dto.OriginalPostId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Post", dto.OriginalPostId);

        var sharedPost = new PostEntity
        {
            AuthorId = userId,
            Content = dto.Content,
            PostType = PostType.Text,
            Visibility = dto.Visibility,
            WallUserId = dto.WallUserId,
            OriginalPostId = dto.OriginalPostId,
            CreatedById = userId
        };

        originalPost.ShareCount++;
        originalPost.UpdatedAt = DateTime.UtcNow;

        db.Posts.Add(sharedPost);
        await db.SaveChangesAsync(ct);

        // Reload shared post with includes for mapping
        var fullSharedPost = await db.Posts
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.Video)
            .Include(p => p.OriginalPost)
                .ThenInclude(op => op!.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.OriginalPost)
                .ThenInclude(op => op!.Video)
            .FirstAsync(p => p.Id == sharedPost.Id, ct);

        return await MapToPostDtoAsync(fullSharedPost, ct);
    }

    public async Task ReportPostAsync(Guid userId, Guid postId, ReportPostDto dto, CancellationToken ct = default)
    {
        var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Post", postId);

        // Avoid duplicate open reports from the same user for the same post.
        var alreadyReported = await db.Reports
            .AnyAsync(r => r.PostId == postId && r.ReportedById == userId && !r.IsResolved, ct);
        if (alreadyReported) return;

        db.Reports.Add(new PostReport
        {
            PostId = postId,
            ReportedById = userId,
            Reason = dto.Reason,
            CreatedById = userId
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task IncrementCommentCountAsync(Guid postId, CancellationToken ct = default)
    {
        await db.Posts
            .Where(p => p.Id == postId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CommentCount, p => p.CommentCount + 1), ct);
    }

    public async Task IncrementReactionCountAsync(Guid postId, CancellationToken ct = default)
    {
        await db.Posts
            .Where(p => p.Id == postId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ReactionCount, p => p.ReactionCount + 1), ct);
    }

    public async Task DecrementCommentCountAsync(Guid postId, CancellationToken ct = default)
    {
        await db.Posts
            .Where(p => p.Id == postId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CommentCount, p => Math.Max(0, p.CommentCount - 1)), ct);
    }

    public async Task DecrementReactionCountAsync(Guid postId, CancellationToken ct = default)
    {
        await db.Posts
            .Where(p => p.Id == postId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ReactionCount, p => Math.Max(0, p.ReactionCount - 1)), ct);
    }

    private async Task<PostDto> MapToPostDtoAsync(PostEntity post, CancellationToken ct)
    {
        var author = await userManager.FindByIdAsync(post.AuthorId.ToString());

        var dto = mapper.Map<PostDto>(post);

        dto.Author = author is null
            ? new PostAuthorDto { Id = post.AuthorId }
            : new PostAuthorDto
            {
                Id = author.Id,
                FullName = author.FullName,
                ProfilePictureUrl = author.ProfilePictureUrl,
                UserName = author.UserName ?? string.Empty
            };

        if (post.OriginalPost is not null)
        {
            dto.OriginalPost = await MapToPostDtoAsync(post.OriginalPost, ct);
        }

        return dto;
    }
}
