using LinkUp.BuildingBlocks.Common.Pagination;

namespace LinkUp.Modules.Search.DTOs;

public class UserSearchResultDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public int MutualFriendsCount { get; set; }
}

public class PostSearchResultDto
{
    public Guid Id { get; set; }
    public string? Content { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GroupSearchResultDto
{
    public Guid ChatId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GroupPhotoUrl { get; set; }
    public int MemberCount { get; set; }
}

public class GlobalSearchResultDto
{
    public PagedResult<UserSearchResultDto> Users { get; set; } = new();
    public PagedResult<PostSearchResultDto> Posts { get; set; } = new();
    public PagedResult<GroupSearchResultDto> Groups { get; set; } = new();
    public int TotalResults { get; set; }
}
