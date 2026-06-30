namespace LinkUp.Modules.UserProfile.DTOs;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    // FirstName/LastName live on ApplicationUser; populated by the manager.
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Flat alias the Angular client reads/sends.
    public DateTime? DateOfBirth => Birthday;
}
