using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.VideoCall.Entities;

public class CallParticipant : BaseEntity
{
    public Guid CallId { get; set; }
    public Guid UserId { get; set; }
    public DateTime? JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }

    /// <summary>
    /// Participant status: "Invited" | "Joined" | "Declined" | "Missed"
    /// </summary>
    public string Status { get; set; } = "Invited";

    public Call Call { get; set; } = null!;
}
