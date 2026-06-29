using LinkUp.BuildingBlocks.Common.Entities;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.VideoCall.Entities;

public class Call : AuditableEntity
{
    public Guid InitiatedById { get; set; }
    public CallType Type { get; set; }
    public CallStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? DurationSeconds { get; set; }

    public ICollection<CallParticipant> Participants { get; set; } = [];
}
