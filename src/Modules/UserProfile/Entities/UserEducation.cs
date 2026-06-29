using LinkUp.BuildingBlocks.Common.Entities;

namespace LinkUp.Modules.UserProfile.Entities;

public class UserEducation : AuditableEntity
{
    public Guid UserId { get; set; }
    public string School { get; set; } = string.Empty;
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public int StartYear { get; set; }
    public int? EndYear { get; set; }
    public bool IsCurrent { get; set; }
}
