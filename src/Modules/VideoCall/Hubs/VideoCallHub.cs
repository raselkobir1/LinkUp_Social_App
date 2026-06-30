using System.Collections.Concurrent;
using System.Security.Claims;
using LinkUp.Modules.VideoCall.Interfaces;
using LinkUp.SharedKernel.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LinkUp.Modules.VideoCall.Hubs;

/// <summary>
/// Unified mesh signaling for 1:1 and group calls. Each participant joins the
/// SignalR group named by the callId; newcomers receive the list of existing
/// participants and send each an offer (deterministic, glare-free). WebRTC SDP/ICE
/// are relayed to a specific user, tagged with the sender so the client routes them
/// to the correct peer connection. Calls are persisted for history via the manager.
/// </summary>
[Authorize]
public class VideoCallHub(IVideoCallManager calls) : Hub
{
    // callId -> (connectionId -> userId)
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _rooms = new();

    private string Uid => Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private static Guid? G(string s) => Guid.TryParse(s, out var g) ? g : null;

    // History persistence is best-effort and must never break live signaling.
    private static async Task SafeAsync(Func<Task> op) { try { await op(); } catch { /* ignore */ } }

    public override async Task OnConnectedAsync()
    {
        // Personal group so a user can be rung on any of their connections.
        await Groups.AddToGroupAsync(Context.ConnectionId, Uid);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var (callId, members) in _rooms)
            if (members.ContainsKey(Context.ConnectionId))
                await LeaveInternalAsync(callId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Ring the invited users. mediaType = "video" | "audio".</summary>
    public async Task StartCall(string callId, string[] inviteeIds, string mediaType, bool isGroup)
    {
        foreach (var id in inviteeIds)
            await Clients.Group(id).SendAsync("CallRinging", callId, Uid, mediaType, isGroup);

        if (G(callId) is Guid cid && G(Uid) is Guid caller)
        {
            var ids = inviteeIds.Select(G).Where(g => g is not null).Select(g => g!.Value);
            await SafeAsync(() => calls.RecordCallStartAsync(cid, caller, ids, isGroup ? CallType.Group : CallType.OneToOne));
        }
    }

    /// <summary>Ring one more user into an already-running call (turns it into a group call).</summary>
    public async Task InviteToCall(string callId, string inviteeId, string mediaType)
    {
        await Clients.Group(inviteeId).SendAsync("CallRinging", callId, Uid, mediaType, true);
        if (G(callId) is Guid cid && G(inviteeId) is Guid invitee)
            await SafeAsync(() => calls.RecordInviteAsync(cid, invitee));
    }

    public async Task JoinCall(string callId)
    {
        var members = _rooms.GetOrAdd(callId, _ => new());
        var existing = members.Values.Where(u => u != Uid).Distinct().ToArray();
        members[Context.ConnectionId] = Uid;

        await Groups.AddToGroupAsync(Context.ConnectionId, callId);

        // Tell the newcomer who is already here (they will offer to each).
        await Clients.Caller.SendAsync("ExistingParticipants", existing);
        await Clients.OthersInGroup(callId).SendAsync("ParticipantJoined", Uid);

        if (G(callId) is Guid cid && G(Uid) is Guid uid)
            await SafeAsync(() => calls.RecordJoinAsync(cid, uid));
    }

    public Task LeaveCall(string callId) => LeaveInternalAsync(callId);

    private async Task LeaveInternalAsync(string callId)
    {
        if (_rooms.TryGetValue(callId, out var members))
        {
            members.TryRemove(Context.ConnectionId, out _);
            if (members.IsEmpty) _rooms.TryRemove(callId, out _);
        }
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, callId);
        await Clients.OthersInGroup(callId).SendAsync("ParticipantLeft", Uid);
        if (G(callId) is Guid cid && G(Uid) is Guid uid)
            await SafeAsync(() => calls.RecordLeaveAsync(cid, uid));
    }

    public async Task DeclineCall(string callId, string callerId)
    {
        await Clients.Group(callerId).SendAsync("CallDeclined", callId, Uid);
        if (G(callId) is Guid cid && G(Uid) is Guid uid)
            await SafeAsync(() => calls.RecordDeclineAsync(cid, uid));
    }

    // ── WebRTC signaling relay (target a user, tag with sender + callId) ──
    public Task SendSdpOffer(string callId, string targetUserId, string sdp) =>
        Clients.Group(targetUserId).SendAsync("SdpOfferReceived", callId, sdp, Uid);

    public Task SendSdpAnswer(string callId, string targetUserId, string sdp) =>
        Clients.Group(targetUserId).SendAsync("SdpAnswerReceived", callId, sdp, Uid);

    public Task SendIceCandidate(string callId, string targetUserId, string candidate) =>
        Clients.Group(targetUserId).SendAsync("IceCandidateReceived", callId, candidate, Uid);

    public Task ToggleCamera(string callId, bool enabled) =>
        Clients.OthersInGroup(callId).SendAsync("CameraToggled", Uid, enabled);

    public Task ToggleMic(string callId, bool enabled) =>
        Clients.OthersInGroup(callId).SendAsync("MicToggled", Uid, enabled);

    public Task StartScreenShare(string callId) =>
        Clients.OthersInGroup(callId).SendAsync("ScreenShareStarted", Uid);

    public Task StopScreenShare(string callId) =>
        Clients.OthersInGroup(callId).SendAsync("ScreenShareStopped", Uid);
}
