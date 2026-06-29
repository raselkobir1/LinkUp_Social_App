using LinkUp.BuildingBlocks.Common.Pagination;

namespace LinkUp.Modules.Admin.DTOs;

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int SuspendedUsers { get; set; }
    public int TotalPosts { get; set; }
    public int TotalReports { get; set; }
    public int NewUsersToday { get; set; }
    public int NewPostsToday { get; set; }
}

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsSuspended { get; set; }
    public string? SuspensionReason { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSeen { get; set; }
    public IList<string> Roles { get; set; } = [];
}

public class SuspendUserDto
{
    public string Reason { get; set; } = string.Empty;
}

public class AdminUserFilterDto : PagedRequest
{
    public string? Search { get; set; }
    public bool? IsSuspended { get; set; }
    public bool? IsActive { get; set; }
}

public class AdminPostDto
{
    public Guid Id { get; set; }
    public string? Content { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ReactionCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsDeleted { get; set; }
}
