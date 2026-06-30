namespace LinkUp.Modules.UserProfile.DTOs;

public class UpdateProfileDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    // The client sends dateOfBirth; Birthday is kept for backward compatibility.
    public DateTime? DateOfBirth { get; set; }
    public DateTime? Birthday { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
}
