using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Admin.DTOs;

namespace LinkUp.Modules.Admin.Interfaces;

public interface IAdminManager
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken ct = default);
    Task<PagedResult<AdminUserDto>> GetUsersAsync(AdminUserFilterDto filter, CancellationToken ct = default);
    Task<AdminUserDto> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    Task SuspendUserAsync(Guid userId, SuspendUserDto dto, CancellationToken ct = default);
    Task UnsuspendUserAsync(Guid userId, CancellationToken ct = default);
    Task DeleteUserAsync(Guid userId, CancellationToken ct = default);
    Task<PagedResult<AdminPostDto>> GetAllPostsAsync(PagedRequest request, CancellationToken ct = default);
    Task AdminDeletePostAsync(Guid postId, CancellationToken ct = default);
    Task<PagedResult<AdminReportDto>> GetReportsAsync(PagedRequest request, bool includeResolved, CancellationToken ct = default);
    Task ResolveReportAsync(Guid reportId, CancellationToken ct = default);
}
