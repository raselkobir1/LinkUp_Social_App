using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Reaction.DTOs;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Reaction.Interfaces;

public interface IReactionManager
{
    Task<ReactionCountDto> AddOrUpdateReactionAsync(Guid userId, AddReactionDto dto, CancellationToken ct = default);
    Task<ReactionCountDto> RemoveReactionAsync(Guid userId, string targetType, Guid targetId, CancellationToken ct = default);
    Task<ReactionCountDto> GetReactionCountsAsync(string targetType, Guid targetId, Guid? viewerId, CancellationToken ct = default);
    Task<PagedResult<ReactorDto>> GetReactorsAsync(string targetType, Guid targetId, ReactionType? filter, PagedRequest request, CancellationToken ct = default);
}
