using Asp.Versioning;
using LinkUp.BuildingBlocks.Common.Controllers;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Search.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Modules.Search.Controllers;

[ApiVersion("1.0")]
public class SearchController(ISearchManager searchManager) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GlobalSearch([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return ApiFail("Search query must be at least 2 characters.", 400);

        var result = await searchManager.GlobalSearchAsync(q, ct);
        return ApiOk(result);
    }

    [HttpGet("users")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var request = new PagedRequest { PageNumber = page, PageSize = pageSize };
        var result = await searchManager.SearchUsersAsync(q, request, ct);
        return ApiOk(result);
    }

    [HttpGet("posts")]
    public async Task<IActionResult> SearchPosts(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var request = new PagedRequest { PageNumber = page, PageSize = pageSize };
        var result = await searchManager.SearchPostsAsync(q, request, ct);
        return ApiOk(result);
    }

    [HttpGet("groups")]
    public async Task<IActionResult> SearchGroups(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var request = new PagedRequest { PageNumber = page, PageSize = pageSize };
        var result = await searchManager.SearchGroupsAsync(q, request, ct);
        return ApiOk(result);
    }
}
