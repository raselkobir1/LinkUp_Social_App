using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Comment.DTOs;

namespace LinkUp.Modules.Comment.Interfaces;

public interface ICommentManager
{
    Task<CommentDto> AddCommentAsync(Guid userId, CreateCommentDto dto, CancellationToken ct = default);
    Task<CommentDto> UpdateCommentAsync(Guid userId, Guid commentId, UpdateCommentDto dto, CancellationToken ct = default);
    Task DeleteCommentAsync(Guid userId, Guid commentId, CancellationToken ct = default);
    Task LikeCommentAsync(Guid userId, Guid commentId, CancellationToken ct = default);
    Task UnlikeCommentAsync(Guid userId, Guid commentId, CancellationToken ct = default);
    Task<PagedResult<CommentDto>> GetPostCommentsAsync(Guid postId, Guid viewerId, PagedRequest request, CancellationToken ct = default);
    Task<PagedResult<CommentDto>> GetRepliesAsync(Guid commentId, Guid viewerId, PagedRequest request, CancellationToken ct = default);
}
