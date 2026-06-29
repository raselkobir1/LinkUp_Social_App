namespace LinkUp.Modules.UserProfile.DTOs;

public class EducationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string School { get; set; } = string.Empty;
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public int StartYear { get; set; }
    public int? EndYear { get; set; }
    public bool IsCurrent { get; set; }
}

public class CreateEducationDto
{
    public string School { get; set; } = string.Empty;
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public int StartYear { get; set; }
    public int? EndYear { get; set; }
    public bool IsCurrent { get; set; }
}

public class UpdateEducationDto
{
    public string School { get; set; } = string.Empty;
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public int StartYear { get; set; }
    public int? EndYear { get; set; }
    public bool IsCurrent { get; set; }
}
