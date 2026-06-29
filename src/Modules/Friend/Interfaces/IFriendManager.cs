using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Friend.DTOs;

namespace LinkUp.Modules.Friend.Interfaces;

public interface IFriendManager
{
    Task SendRequestAsync(Guid senderId, SendFriendRequestDto dto, CancellationToken ct = default);
    Task AcceptRequestAsync(Guid requestId, Guid userId, CancellationToken ct = default);
    Task RejectRequestAsync(Guid requestId, Guid userId, CancellationToken ct = default);
    Task CancelRequestAsync(Guid requestId, Guid userId, CancellationToken ct = default);
    Task UnfriendAsync(Guid userId, Guid friendId, CancellationToken ct = default);

    Task<PagedResult<FriendDto>> GetFriendListAsync(Guid userId, PagedRequest request, CancellationToken ct = default);
    Task<PagedResult<FriendRequestDto>> GetPendingRequestsAsync(Guid userId, PagedRequest request, CancellationToken ct = default);
    Task<PagedResult<FriendRequestDto>> GetSentRequestsAsync(Guid userId, PagedRequest request, CancellationToken ct = default);

    Task<List<UserCardDto>> GetMutualFriendsAsync(Guid userId, Guid otherUserId, CancellationToken ct = default);
    Task<FriendshipStatusDto> GetFriendshipStatusAsync(Guid userId, Guid otherUserId, CancellationToken ct = default);
    Task<List<UserCardDto>> GetFriendSuggestionsAsync(Guid userId, int count, CancellationToken ct = default);
    Task<bool> IsFriendAsync(Guid userId, Guid otherUserId, CancellationToken ct = default);
}
