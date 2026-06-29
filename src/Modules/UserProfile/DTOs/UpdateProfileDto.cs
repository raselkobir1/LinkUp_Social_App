namespace LinkUp.Modules.UserProfile.DTOs;

public class UpdateProfileDto
{
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
}
