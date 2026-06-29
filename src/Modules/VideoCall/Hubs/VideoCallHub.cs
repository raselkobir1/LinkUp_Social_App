using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LinkUp.Modules.VideoCall.Hubs;

[Authorize]
public class VideoCallHub : Hub
{
    private string CurrentUserId => Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public override async Task OnConnectedAsync()
    {
        // Join personal group for receiving incoming call notifications
        await Groups.AddToGroupAsync(Context.ConnectionId, CurrentUserId);
        await base.OnConnectedAsync();
    }

    // WebRTC signaling relay methods:

    public async Task InitiateCall(string targetUserId, string callId, string callType)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, callId);
        await Clients.Group(targetUserId).SendAsync("CallInitiated", callId, CurrentUserId, callType);
    }

    /// <summary>Start a group call: the caller joins the call group and every invited
    /// member is rung. Each participant later calls JoinCall to enter the mesh.</summary>
    public async Task InitiateGroupCall(string callId, string[] memberIds, string callType)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, callId);
        foreach (var memberId in memberIds)
            await Clients.Group(memberId).SendAsync("GroupCallInitiated", callId, CurrentUserId, callType, memberIds);
    }

    /// <summary>A participant joins an in-progress call. Existing members are told to
    /// open a peer connection (WebRTC mesh) with the joiner.</summary>
    public async Task JoinCall(string callId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, callId);
        await Clients.OthersInGroup(callId).SendAsync("ParticipantJoined", callId, CurrentUserId);
    }

    /// <summary>A participant leaves a group call (without ending it for everyone).</summary>
    public async Task LeaveCall(string callId)
    {
        await Clients.OthersInGroup(callId).SendAsync("ParticipantLeft", callId, CurrentUserId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, callId);
    }

    public async Task AcceptCall(string callerId, string callId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, callId);
        await Clients.Group(callerId).SendAsync("CallAccepted", callId);
    }

    public async Task DeclineCall(string callerId, string callId) =>
        await Clients.Group(callerId).SendAsync("CallDeclined", callId);

    public async Task EndCall(string callId) =>
        await Clients.Group(callId).SendAsync("CallEnded", callId);

    public async Task SendIceCandidate(string targetUserId, string candidate) =>
        await Clients.Group(targetUserId).SendAsync("IceCandidateReceived", candidate, CurrentUserId);

    public async Task SendSdpOffer(string targetUserId, string sdp) =>
        await Clients.Group(targetUserId).SendAsync("SdpOfferReceived", sdp, CurrentUserId);

    public async Task SendSdpAnswer(string targetUserId, string sdp) =>
        await Clients.Group(targetUserId).SendAsync("SdpAnswerReceived", sdp, CurrentUserId);

    public async Task ToggleCamera(string callId, bool enabled) =>
        await Clients.OthersInGroup(callId).SendAsync("CameraToggled", CurrentUserId, enabled);

    public async Task ToggleMic(string callId, bool enabled) =>
        await Clients.OthersInGroup(callId).SendAsync("MicToggled", CurrentUserId, enabled);

    public async Task StartScreenShare(string callId) =>
        await Clients.OthersInGroup(callId).SendAsync("ScreenShareStarted", CurrentUserId);

    public async Task StopScreenShare(string callId) =>
        await Clients.OthersInGroup(callId).SendAsync("ScreenShareStopped", CurrentUserId);
}
