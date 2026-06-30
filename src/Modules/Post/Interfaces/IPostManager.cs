using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Post.DTOs;

namespace LinkUp.Modules.Post.Interfaces;

public interface IPostManager
{
    Task<PostDto> CreatePostAsync(Guid userId, CreatePostDto dto, CancellationToken ct = default);
    Task<PostDto> UpdatePostAsync(Guid userId, Guid postId, UpdatePostDto dto, CancellationToken ct = default);
    Task DeletePostAsync(Guid userId, Guid postId, CancellationToken ct = default);
    Task<PostDto> PinPostAsync(Guid userId, Guid postId, bool pin, CancellationToken ct = default);
    Task<PostDto> GetPostByIdAsync(Guid postId, Guid viewerId, CancellationToken ct = default);
    Task<PagedResult<PostDto>> GetWallPostsAsync(Guid wallUserId, Guid viewerId, PagedRequest request, CancellationToken ct = default);
    Task<PagedResult<PostDto>> GetFeedAsync(Guid userId, PagedRequest request, CancellationToken ct = default);
    Task<PostDto> SharePostAsync(Guid userId, SharePostDto dto, CancellationToken ct = default);
    Task ReportPostAsync(Guid userId, Guid postId, ReportPostDto dto, CancellationToken ct = default);
    Task IncrementCommentCountAsync(Guid postId, CancellationToken ct = default);
    Task IncrementReactionCountAsync(Guid postId, CancellationToken ct = default);
    Task DecrementCommentCountAsync(Guid postId, CancellationToken ct = default);
    Task DecrementReactionCountAsync(Guid postId, CancellationToken ct = default);
}
