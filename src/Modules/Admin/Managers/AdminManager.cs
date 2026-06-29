using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.BuildingBlocks.Infrastructure.Extensions;
using LinkUp.Modules.Admin.DTOs;
using LinkUp.Modules.Admin.Interfaces;
using LinkUp.Modules.Identity.Configuration;
using LinkUp.Modules.Identity.Entities;
using LinkUp.Modules.Post.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.Admin.Managers;

public class AdminManager(
    UserManager<ApplicationUser> userManager,
    IdentityDbContext identityDbContext,
    PostDbContext postDbContext) : IAdminManager
{
    public async Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        var totalUsers = await identityDbContext.Users.CountAsync(ct);
        var activeUsers = await identityDbContext.Users.CountAsync(u => u.IsActive && !u.IsSuspended, ct);
        var suspendedUsers = await identityDbContext.Users.CountAsync(u => u.IsSuspended, ct);
        var newUsersToday = await identityDbContext.Users.CountAsync(u => u.CreatedAt >= today, ct);
        var totalPosts = await postDbContext.Posts.CountAsync(p => !p.IsDeleted, ct);
        var newPostsToday = await postDbContext.Posts.CountAsync(p => !p.IsDeleted && p.CreatedAt >= today, ct);

        return new DashboardStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            SuspendedUsers = suspendedUsers,
            TotalPosts = totalPosts,
            TotalReports = 0,
            NewUsersToday = newUsersToday,
            NewPostsToday = newPostsToday
        };
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUserFilterDto filter, CancellationToken ct = default)
    {
        var query = identityDbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search) ||
                u.Email!.ToLower().Contains(search) ||
                u.UserName!.ToLower().Contains(search));
        }

        if (filter.IsSuspended.HasValue)
            query = query.Where(u => u.IsSuspended == filter.IsSuspended.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(u => u.IsActive == filter.IsActive.Value);

        query = query.OrderByDescending(u => u.CreatedAt);

        var pagedUsers = await query.ToPagedResultAsync(filter, ct);

        var userDtos = new List<AdminUserDto>();
        foreach (var user in pagedUsers.Items)
        {
            var roles = await userManager.GetRolesAsync(user);
            userDtos.Add(MapToAdminUserDto(user, roles));
        }

        return PagedResult<AdminUserDto>.Create(userDtos, pagedUsers.TotalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<AdminUserDto> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User", userId);

        var roles = await userManager.GetRolesAsync(user);
        return MapToAdminUserDto(user, roles);
    }

    public async Task SuspendUserAsync(Guid userId, SuspendUserDto dto, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User", userId);

        user.IsSuspended = true;
        user.SuspensionReason = dto.Reason;
        user.UpdatedAt = DateTime.UtcNow;

        await userManager.UpdateAsync(user);
    }

    public async Task UnsuspendUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User", userId);

        user.IsSuspended = false;
        user.SuspensionReason = null;
        user.UpdatedAt = DateTime.UtcNow;

        await userManager.UpdateAsync(user);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User", userId);

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await userManager.UpdateAsync(user);
    }

    public async Task<PagedResult<AdminPostDto>> GetAllPostsAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = postDbContext.Posts
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt);

        var paged = await query.ToPagedResultAsync(request, ct);

        var userIds = paged.Items.Select(p => p.AuthorId).Distinct().ToList();
        var users = await identityDbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var dtos = paged.Items.Select(p => new AdminPostDto
        {
            Id = p.Id,
            Content = p.Content,
            AuthorId = p.AuthorId,
            AuthorName = users.TryGetValue(p.AuthorId, out var u) ? u.FullName : "Unknown",
            CreatedAt = p.CreatedAt,
            ReactionCount = p.ReactionCount,
            CommentCount = p.CommentCount,
            IsDeleted = p.IsDeleted
        });

        return PagedResult<AdminPostDto>.Create(dtos, paged.TotalCount, request.PageNumber, request.PageSize);
    }

    public async Task AdminDeletePostAsync(Guid postId, CancellationToken ct = default)
    {
        var post = await postDbContext.Posts.FindAsync([postId], ct)
            ?? throw new NotFoundException("Post", postId);

        post.IsDeleted = true;
        post.DeletedAt = DateTime.UtcNow;
        await postDbContext.SaveChangesAsync(ct);
    }

    private static AdminUserDto MapToAdminUserDto(ApplicationUser user, IList<string> roles) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email!,
        UserName = user.UserName!,
        ProfilePictureUrl = user.ProfilePictureUrl,
        IsActive = user.IsActive,
        IsSuspended = user.IsSuspended,
        SuspensionReason = user.SuspensionReason,
        EmailConfirmed = user.EmailConfirmed,
        CreatedAt = user.CreatedAt,
        LastSeen = user.LastSeen,
        Roles = roles
    };
}
