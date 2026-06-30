using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.VideoCall.DTOs;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.VideoCall.Interfaces;

public interface IVideoCallManager
{
    Task<CallDto> InitiateCallAsync(Guid callerId, InitiateCallDto dto, CancellationToken ct = default);
    Task<CallDto> AcceptCallAsync(Guid userId, Guid callId, CancellationToken ct = default);
    Task DeclineCallAsync(Guid userId, Guid callId, CancellationToken ct = default);
    Task EndCallAsync(Guid userId, Guid callId, CancellationToken ct = default);
    Task<PagedResult<CallHistoryDto>> GetCallHistoryAsync(Guid userId, PagedRequest request, CancellationToken ct = default);
    Task<CallDto?> GetActiveCallAsync(Guid userId, CancellationToken ct = default);

    // --- Recording hooks called by the SignalR hub (persist for call history) ---
    Task RecordCallStartAsync(Guid callId, Guid callerId, IEnumerable<Guid> inviteeIds, CallType type, CancellationToken ct = default);
    Task RecordInviteAsync(Guid callId, Guid inviteeId, CancellationToken ct = default);
    Task RecordJoinAsync(Guid callId, Guid userId, CancellationToken ct = default);
    Task RecordDeclineAsync(Guid callId, Guid userId, CancellationToken ct = default);
    Task RecordLeaveAsync(Guid callId, Guid userId, CancellationToken ct = default);
}
