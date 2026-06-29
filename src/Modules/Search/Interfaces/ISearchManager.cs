using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Search.DTOs;

namespace LinkUp.Modules.Search.Interfaces;

public interface ISearchManager
{
    Task<PagedResult<UserSearchResultDto>> SearchUsersAsync(string query, PagedRequest request, CancellationToken ct = default);
    Task<PagedResult<PostSearchResultDto>> SearchPostsAsync(string query, PagedRequest request, CancellationToken ct = default);
    Task<PagedResult<GroupSearchResultDto>> SearchGroupsAsync(string query, PagedRequest request, CancellationToken ct = default);
    Task<GlobalSearchResultDto> GlobalSearchAsync(string query, CancellationToken ct = default);
}
