using AutoMapper;
using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.BuildingBlocks.Infrastructure.Extensions;
using LinkUp.Modules.Comment.Configuration;
using LinkUp.Modules.Comment.DTOs;
using LinkUp.Modules.Comment.Interfaces;
using LinkUp.Modules.Identity.Entities;
using LinkUp.Modules.Notification.DTOs;
using LinkUp.Modules.Notification.Interfaces;
using LinkUp.SharedKernel.Enums;
using LinkUp.SharedKernel.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CommentEntity = LinkUp.Modules.Comment.Entities.Comment;
using CommentLikeEntity = LinkUp.Modules.Comment.Entities.CommentLike;

namespace LinkUp.Modules.Comment.Managers;

public class CommentManager(
    CommentDbContext db,
    UserManager<ApplicationUser> userManager,
    INotificationManager notificationManager,
    LinkUp.Modules.Post.Configuration.PostDbContext postDb,
    IMapper mapper) : ICommentManager
{
    private async Task NotifyMentionsAsync(string? content, Guid authorId, Guid postId, CancellationToken ct)
    {
        foreach (var username in MentionParser.ExtractUsernames(content))
        {
            var mentioned = await userManager.FindByNameAsync(username);
            if (mentioned is null || mentioned.Id == authorId) continue;
            await notificationManager.CreateNotificationAsync(new CreateNotificationDto(
                mentioned.Id, authorId, NotificationType.Mention,
                postId, "Post", "mentioned you in a comment"), ct);
        }
    }

    private async Task<CommentDto> MapToCommentDtoAsync(CommentEntity comment, Guid viewerId, CancellationToken ct = default)
    {
        var author = await userManager.FindByIdAsync(comment.AuthorId.ToString());

        var isLiked = viewerId != Guid.Empty && await db.CommentLikes
            .AnyAsync(l => l.CommentId == comment.Id && l.UserId == viewerId, ct);

        var dto = mapper.Map<CommentDto>(comment);

        dto.Author = author is not null
            ? new CommentAuthorDto
            {
                Id = author.Id,
                FullName = author.FullName,
                ProfilePictureUrl = author.ProfilePictureUrl,
                UserName = author.UserName ?? string.Empty
            }
            : new CommentAuthorDto { Id = comment.AuthorId };

        dto.IsLikedByCurrentUser = isLiked;

        return dto;
    }

    public async Task<CommentDto> AddCommentAsync(Guid userId, CreateCommentDto dto, CancellationToken ct = default)
    {
        var comment = new CommentEntity
        {
            PostId = dto.PostId,
            AuthorId = userId,
            Content = dto.Content,
            ParentCommentId = dto.ParentCommentId
        };

        Guid? parentAuthorId = null;
        if (dto.ParentCommentId.HasValue)
        {
            var parent = await db.Comments
                .FirstOrDefaultAsync(c => c.Id == dto.ParentCommentId.Value && !c.IsDeleted, ct)
                ?? throw new NotFoundException("Comment", dto.ParentCommentId.Value);

            parent.ReplyCount++;
            parentAuthorId = parent.AuthorId;
        }

        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        var actor = await userManager.FindByIdAsync(userId.ToString());
        var actorName = actor?.FullName ?? "Someone";

        if (parentAuthorId is { } pa && pa != userId)
        {
            // Reply → notify the parent comment's author.
            await notificationManager.CreateNotificationAsync(new CreateNotificationDto(
                pa, userId, NotificationType.CommentReply,
                dto.PostId, "Post", $"{actorName} replied to your comment"), ct);
        }
        else
        {
            // Top-level comment → notify the post author.
            var postAuthorId = await postDb.Posts
                .Where(p => p.Id == dto.PostId && !p.IsDeleted)
                .Select(p => (Guid?)p.AuthorId)
                .FirstOrDefaultAsync(ct);
            if (postAuthorId is { } pAuthor && pAuthor != userId)
                await notificationManager.CreateNotificationAsync(new CreateNotificationDto(
                    pAuthor, userId, NotificationType.PostComment,
                    dto.PostId, "Post", $"{actorName} commented on your post"), ct);
        }

        await NotifyMentionsAsync(dto.Content, userId, dto.PostId, ct);

        return await MapToCommentDtoAsync(comment, userId, ct);
    }

    public async Task<CommentDto> UpdateCommentAsync(Guid userId, Guid commentId, UpdateCommentDto dto, CancellationToken ct = default)
    {
        var comment = await db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, ct)
            ?? throw new NotFoundException("Comment", commentId);

        if (comment.AuthorId != userId)
            throw new ForbiddenException("You are not allowed to edit this comment.");

        comment.Content = dto.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return await MapToCommentDtoAsync(comment, userId, ct);
    }

    public async Task DeleteCommentAsync(Guid userId, Guid commentId, CancellationToken ct = default)
    {
        var comment = await db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, ct)
            ?? throw new NotFoundException("Comment", commentId);

        if (comment.AuthorId != userId)
            throw new ForbiddenException("You are not allowed to delete this comment.");

        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;

        if (comment.ParentCommentId.HasValue)
        {
            var parent = await db.Comments
                .FirstOrDefaultAsync(c => c.Id == comment.ParentCommentId.Value && !c.IsDeleted, ct);

            if (parent is not null && parent.ReplyCount > 0)
                parent.ReplyCount--;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task LikeCommentAsync(Guid userId, Guid commentId, CancellationToken ct = default)
    {
        var comment = await db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, ct)
            ?? throw new NotFoundException("Comment", commentId);

        var alreadyLiked = await db.CommentLikes
            .AnyAsync(l => l.CommentId == commentId && l.UserId == userId, ct);

        if (alreadyLiked)
            throw new ConflictException("Already liked");

        db.CommentLikes.Add(new CommentLikeEntity
        {
            CommentId = commentId,
            UserId = userId
        });

        comment.LikeCount++;

        await db.SaveChangesAsync(ct);
    }

    public async Task UnlikeCommentAsync(Guid userId, Guid commentId, CancellationToken ct = default)
    {
        var like = await db.CommentLikes
            .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId, ct)
            ?? throw new NotFoundException("Like not found.");

        db.CommentLikes.Remove(like);

        var comment = await db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, ct);

        if (comment is not null && comment.LikeCount > 0)
            comment.LikeCount--;

        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<CommentDto>> GetPostCommentsAsync(Guid postId, Guid viewerId, PagedRequest request, CancellationToken ct = default)
    {
        var query = db.Comments
            .AsNoTracking()
            .Where(c => c.PostId == postId && c.ParentCommentId == null && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt);

        var pagedComments = await query.ToPagedResultAsync(request, ct);

        var dtos = new List<CommentDto>();
        foreach (var comment in pagedComments.Items)
        {
            var dto = await MapToCommentDtoAsync(comment, viewerId, ct);
            dtos.Add(dto);
        }

        return PagedResult<CommentDto>.Create(dtos, pagedComments.TotalCount, pagedComments.PageNumber, pagedComments.PageSize);
    }

    public async Task<PagedResult<CommentDto>> GetRepliesAsync(Guid commentId, Guid viewerId, PagedRequest request, CancellationToken ct = default)
    {
        _ = await db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, ct)
            ?? throw new NotFoundException("Comment", commentId);

        var query = db.Comments
            .AsNoTracking()
            .Where(c => c.ParentCommentId == commentId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt);

        var pagedReplies = await query.ToPagedResultAsync(request, ct);

        var dtos = new List<CommentDto>();
        foreach (var comment in pagedReplies.Items)
        {
            var dto = await MapToCommentDtoAsync(comment, viewerId, ct);
            dtos.Add(dto);
        }

        return PagedResult<CommentDto>.Create(dtos, pagedReplies.TotalCount, pagedReplies.PageNumber, pagedReplies.PageSize);
    }
}
