using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.UserProfile.Entities;

public class UserExperience : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Description { get; set; }
}
