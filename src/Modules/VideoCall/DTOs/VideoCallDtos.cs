using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.VideoCall.DTOs;

public record InitiateCallDto(Guid TargetUserId, CallType Type);

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
