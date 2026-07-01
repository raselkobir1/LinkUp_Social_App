using System.Text.Json.Serialization;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.VideoCall.DTOs;

public record InitiateCallDto(Guid TargetUserId, CallType Type);

/// <summary>
/// A WebRTC ICE server. Shape matches the browser's RTCIceServer so the client can
/// pass the list straight into `new RTCPeerConnection({ iceServers })`.
/// </summary>
public class IceServerDto
{
    [JsonConverter(typeof(StringOrArrayConverter))]
    public List<string> Urls { get; set; } = [];
    public string? Username { get; set; }
    public string? Credential { get; set; }
}

public class CallParticipantDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? JoinedAt { get; set; }
}

public class CallDto
{
    public Guid Id { get; set; }
    public Guid InitiatedById { get; set; }
    public CallType Type { get; set; }
    public CallStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? DurationSeconds { get; set; }
    public List<CallParticipantDto> Participants { get; set; } = [];
}

public class CallHistoryDto : CallDto
{
    public bool IsIncoming { get; set; }
}
