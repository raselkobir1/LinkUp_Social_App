using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.VideoCall.DTOs;

namespace LinkUp.Modules.VideoCall.Interfaces;

public interface IVideoCallManager
{
    Task<CallDto> InitiateCallAsync(Guid callerId, InitiateCallDto dto, CancellationToken ct = default);
    Task<CallDto> AcceptCallAsync(Guid userId, Guid callId, CancellationToken ct = default);
    Task DeclineCallAsync(Guid userId, Guid callId, CancellationToken ct = default);
    Task EndCallAsync(Guid userId, Guid callId, CancellationToken ct = default);
    Task<PagedResult<CallHistoryDto>> GetCallHistoryAsync(Guid userId, PagedRequest request, CancellationToken ct = default);
    Task<CallDto?> GetActiveCallAsync(Guid userId, CancellationToken ct = default);
}
