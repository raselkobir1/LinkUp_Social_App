using Asp.Versioning;
using LinkUp.BuildingBlocks.Common.Controllers;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Admin.DTOs;
using LinkUp.Modules.Admin.Interfaces;
using LinkUp.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Modules.Admin.Controllers;

[ApiVersion("1.0")]
[Authorize(Roles = AppConstants.Roles.Admin)]
public class AdminController(IAdminManager adminManager) : BaseApiController
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var stats = await adminManager.GetDashboardStatsAsync(ct);
        return ApiOk(stats);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUserFilterDto filter, CancellationToken ct)
    {
        var result = await adminManager.GetUsersAsync(filter, ct);
        return ApiOk(result);
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken ct)
    {
        var user = await adminManager.GetUserByIdAsync(userId, ct);
        return ApiOk(user);
    }

    [HttpPut("users/{userId:guid}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid userId, [FromBody] SuspendUserDto dto, CancellationToken ct)
    {
        await adminManager.SuspendUserAsync(userId, dto, ct);
        return ApiOk<object>(null!, "User suspended.");
    }

    [HttpPut("users/{userId:guid}/unsuspend")]
    public async Task<IActionResult> UnsuspendUser(Guid userId, CancellationToken ct)
    {
        await adminManager.UnsuspendUserAsync(userId, ct);
        return ApiOk<object>(null!, "User unsuspended.");
    }

    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken ct)
    {
        await adminManager.DeleteUserAsync(userId, ct);
        return ApiOk<object>(null!, "User deactivated.");
    }

    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var request = new PagedRequest { PageNumber = page, PageSize = pageSize };
        var result = await adminManager.GetAllPostsAsync(request, ct);
        return ApiOk(result);
    }

    [HttpDelete("posts/{postId:guid}")]
    public async Task<IActionResult> DeletePost(Guid postId, CancellationToken ct)
    {
        await adminManager.AdminDeletePostAsync(postId, ct);
        return ApiOk<object>(null!, "Post deleted.");
    }
}
