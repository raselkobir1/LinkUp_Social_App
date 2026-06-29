using AutoMapper;
using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.BuildingBlocks.Infrastructure.Extensions;
using LinkUp.Modules.Identity.Entities;
using LinkUp.Modules.VideoCall.Configuration;
using LinkUp.Modules.VideoCall.DTOs;
using LinkUp.Modules.VideoCall.Entities;
using LinkUp.Modules.VideoCall.Hubs;
using LinkUp.Modules.VideoCall.Interfaces;
using LinkUp.SharedKernel.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.VideoCall.Managers;

public class VideoCallManager(
    VideoCallDbContext db,
    UserManager<ApplicationUser> userManager,
    IHubContext<VideoCallHub> hubContext,
    IMapper mapper) : IVideoCallManager
{
    public async Task<CallDto> InitiateCallAsync(Guid callerId, InitiateCallDto dto, CancellationToken ct = default)
    {
        var targetUser = await userManager.FindByIdAsync(dto.TargetUserId.ToString())
            ?? throw new NotFoundException("User", dto.TargetUserId);

        var call = new Call
        {
            InitiatedById = callerId,
            Type = dto.Type,
            Status = CallStatus.Initiated,
            CreatedById = callerId
        };

        var callerParticipant = new CallParticipant
        {
            CallId = call.Id,
            UserId = callerId,
            Status = "Joined",
            JoinedAt = DateTime.UtcNow
        };

        var targetParticipant = new CallParticipant
        {
            CallId = call.Id,
            UserId = dto.TargetUserId,
            Status = "Invited"
        };

        call.Participants.Add(callerParticipant);
        call.Participants.Add(targetParticipant);

        db.Calls.Add(call);
        await db.SaveChangesAsync(ct);

        // Notify the target user via their personal SignalR group
        await hubContext.Clients
            .Group(dto.TargetUserId.ToString())
            .SendAsync("CallInitiated", call.Id, callerId, dto.Type.ToString(), cancellationToken: ct);

        return await BuildCallDtoAsync(call, ct);
    }

    public async Task<CallDto> AcceptCallAsync(Guid userId, Guid callId, CancellationToken ct = default)
    {
        var call = await db.Calls
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == callId && !c.IsDeleted, ct)
            ?? throw new NotFoundException("Call", callId);

        if (call.Status != CallStatus.Initiated && call.Status != CallStatus.Ongoing)
            throw new ConflictException("The call is no longer active and cannot be accepted.");

        var participant = call.Participants.FirstOrDefault(p => p.UserId == userId)
            ?? throw new ForbiddenException("You are not a participant of this call.");

        if (participant.Status == "Joined")
            throw new ConflictException("You have already joined this call.");

        participant.Status = "Joined";
        participant.JoinedAt = DateTime.UtcNow;

        // Transition to Ongoing only on first accept (when still Initiated)
        if (call.Status == CallStatus.Initiated)
        {
            call.Status = CallStatus.Ongoing;
            call.StartedAt = DateTime.UtcNow;
            call.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        // Notify all participants in the call group
        await hubContext.Clients
            .Group(callId.ToString())
            .SendAsync("CallAccepted", callId, cancellationToken: ct);

        return await BuildCallDtoAsync(call, ct);
    }

    public async Task DeclineCallAsync(Guid userId, Guid callId, CancellationToken ct = default)
    {
        var call = await db.Calls
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == callId && !c.IsDeleted, ct)
            ?? throw new NotFoundException("Call", callId);

        if (call.Status != CallStatus.Initiated && call.Status != CallStatus.Ongoing)
            throw new ConflictException("The call has already ended or been declined.");

        var participant = call.Participants.FirstOrDefault(p => p.UserId == userId)
            ?? throw new ForbiddenException("You are not a participant of this call.");

        participant.Status = "Declined";
        call.UpdatedAt = DateTime.UtcNow;

        // If all non-initiator participants have declined, mark the call as Declined
        var nonInitiatorParticipants = call.Participants
            .Where(p => p.UserId != call.InitiatedById)
            .ToList();

        bool allDeclined = nonInitiatorParticipants.Count > 0
            && nonInitiatorParticipants.All(p => p.Status == "Declined");

        if (allDeclined)
        {
            call.Status = CallStatus.Declined;
        }

        await db.SaveChangesAsync(ct);

        // Notify the caller via their personal SignalR group
        await hubContext.Clients
            .Group(call.InitiatedById.ToString())
            .SendAsync("CallDeclined", callId, cancellationToken: ct);
    }

    public async Task EndCallAsync(Guid userId, Guid callId, CancellationToken ct = default)
    {
        var call = await db.Calls
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == callId && !c.IsDeleted, ct)
            ?? throw new NotFoundException("Call", callId);

        if (call.Status == CallStatus.Ended)
            throw new ConflictException("The call has already ended.");

        // Any participant may end the call
        var isParticipant = call.Participants.Any(p => p.UserId == userId);
        if (!isParticipant)
            throw new ForbiddenException("You are not a participant of this call.");

        var now = DateTime.UtcNow;
        call.Status = CallStatus.Ended;
        call.EndedAt = now;
        call.UpdatedAt = now;

        if (call.StartedAt.HasValue)
        {
            call.DurationSeconds = (int)(now - call.StartedAt.Value).TotalSeconds;
        }

        // Mark all still-active participants as having left
        foreach (var participant in call.Participants.Where(p => p.Status == "Joined" && p.LeftAt == null))
        {
            participant.LeftAt = now;
        }

        await db.SaveChangesAsync(ct);

        // Notify all participants in the call group
        await hubContext.Clients
            .Group(callId.ToString())
            .SendAsync("CallEnded", callId, cancellationToken: ct);
    }

    public async Task<PagedResult<CallHistoryDto>> GetCallHistoryAsync(Guid userId, PagedRequest request, CancellationToken ct = default)
    {
        // Get all call IDs where the user is a participant
        var participantCallIds = await db.CallParticipants
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.CallId)
            .ToListAsync(ct);

        var query = db.Calls
            .AsNoTracking()
            .Where(c => participantCallIds.Contains(c.Id) && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt);

        var pagedCalls = await query.ToPagedResultAsync(request, ct);

        // Load participants for each call and enrich with user info
        var callIds = pagedCalls.Items.Select(c => c.Id).ToList();

        var allParticipants = await db.CallParticipants
            .AsNoTracking()
            .Where(p => callIds.Contains(p.CallId))
            .ToListAsync(ct);

        var dtos = new List<CallHistoryDto>();

        foreach (var call in pagedCalls.Items)
        {
            var callDto = mapper.Map<CallHistoryDto>(call);
            callDto.IsIncoming = call.InitiatedById != userId;

            var callParticipants = allParticipants.Where(p => p.CallId == call.Id).ToList();
            var participantDtos = new List<CallParticipantDto>();

            foreach (var participant in callParticipants)
            {
                var participantDto = mapper.Map<CallParticipantDto>(participant);

                var appUser = await userManager.FindByIdAsync(participant.UserId.ToString());
                if (appUser is not null)
                {
                    participantDto.FullName = appUser.FullName;
                    participantDto.ProfilePictureUrl = appUser.ProfilePictureUrl;
                }

                participantDtos.Add(participantDto);
            }

            callDto.Participants = participantDtos;
            dtos.Add(callDto);
        }

        return PagedResult<CallHistoryDto>.Create(dtos, pagedCalls.TotalCount, request.PageNumber, request.PageSize);
    }

    public async Task<CallDto?> GetActiveCallAsync(Guid userId, CancellationToken ct = default)
    {
        // Use List<> (not arrays) so .Contains binds to List<T>.Contains rather than the
        // ReadOnlySpan<T>.Contains overload, which EF Core cannot translate (throws on query build).
        var activeStatuses = new List<CallStatus> { CallStatus.Initiated, CallStatus.Ongoing };
        var activeParticipantStatuses = new List<string> { "Joined", "Invited" };

        var call = await db.Calls
            .AsNoTracking()
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c =>
                activeStatuses.Contains(c.Status) &&
                !c.IsDeleted &&
                c.Participants.Any(p => p.UserId == userId && activeParticipantStatuses.Contains(p.Status)),
                ct);

        if (call is null)
            return null;

        return await BuildCallDtoAsync(call, ct);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<CallDto> BuildCallDtoAsync(Call call, CancellationToken ct)
    {
        // Ensure participants are loaded
        if (!db.Entry(call).Collection(c => c.Participants).IsLoaded)
        {
            await db.Entry(call).Collection(c => c.Participants).LoadAsync(ct);
        }

        var callDto = mapper.Map<CallDto>(call);

        var participantDtos = new List<CallParticipantDto>();

        foreach (var participant in call.Participants)
        {
            var participantDto = mapper.Map<CallParticipantDto>(participant);

            var appUser = await userManager.FindByIdAsync(participant.UserId.ToString());
            if (appUser is not null)
            {
                participantDto.FullName = appUser.FullName;
                participantDto.ProfilePictureUrl = appUser.ProfilePictureUrl;
            }

            participantDtos.Add(participantDto);
        }

        callDto.Participants = participantDtos;
        return callDto;
    }
}
