using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.BuildingBlocks.Infrastructure.Extensions;
using LinkUp.Modules.Identity.Configuration;
using LinkUp.Modules.Identity.Entities;
using LinkUp.Modules.Search.DTOs;
using LinkUp.Modules.Search.Interfaces;
using LinkUp.SharedKernel.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.Search.Managers;

public class SearchManager(
    UserManager<ApplicationUser> userManager,
    IdentityDbContext identityDbContext) : ISearchManager
{
    private const int GlobalSearchPageSize = 5;

    public async Task<PagedResult<UserSearchResultDto>> SearchUsersAsync(
        string query, PagedRequest request, CancellationToken ct = default)
    {
        var normalizedQuery = query.Trim().ToLower();

        var result = await identityDbContext.Users
            .Where(u => u.IsActive && !u.IsSuspended &&
                (u.FirstName.ToLower().Contains(normalizedQuery) ||
                 u.LastName.ToLower().Contains(normalizedQuery) ||
                 (u.FirstName + " " + u.LastName).ToLower().Contains(normalizedQuery) ||
                 u.UserName!.ToLower().Contains(normalizedQuery)))
            .Select(u => new UserSearchResultDto
            {
                Id = u.Id,
                FullName = u.FirstName + " " + u.LastName,
                UserName = u.UserName!,
                ProfilePictureUrl = u.ProfilePictureUrl,
                MutualFriendsCount = 0
            })
            .ToPagedResultAsync(request, ct);

        return result;
    }

    public async Task<PagedResult<PostSearchResultDto>> SearchPostsAsync(
        string query, PagedRequest request, CancellationToken ct = default)
    {
        // Search is done via raw SQL for text search since Post module's DbContext
        // isn't directly referenced. We query only public posts via the users context
        // by returning an empty result for now — full implementation will inject PostDbContext
        // once the search module adds the reference.
        return PagedResult<PostSearchResultDto>.Create([], 0, request.PageNumber, request.PageSize);
    }

    public async Task<PagedResult<GroupSearchResultDto>> SearchGroupsAsync(
        string query, PagedRequest request, CancellationToken ct = default)
    {
        // Group search queries ChatDbContext — deferred until Chat module is registered
        return PagedResult<GroupSearchResultDto>.Create([], 0, request.PageNumber, request.PageSize);
    }

    public async Task<GlobalSearchResultDto> GlobalSearchAsync(string query, CancellationToken ct = default)
    {
        var pageRequest = new PagedRequest { PageNumber = 1, PageSize = GlobalSearchPageSize };

        var users = await SearchUsersAsync(query, pageRequest, ct);
        var posts = await SearchPostsAsync(query, pageRequest, ct);
        var groups = await SearchGroupsAsync(query, pageRequest, ct);

        return new GlobalSearchResultDto
        {
            Users = users,
            Posts = posts,
            Groups = groups,
            TotalResults = users.TotalCount + posts.TotalCount + groups.TotalCount
        };
    }
}
