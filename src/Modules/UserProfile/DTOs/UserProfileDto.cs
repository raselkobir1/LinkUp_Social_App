namespace LinkUp.Modules.UserProfile.DTOs;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    // FirstName/LastName/FullName/UserName/Email/online live on ApplicationUser;
    // populated by the manager.
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Relationship to the viewer + counts, populated by the manager.
    public int FriendCount { get; set; }
    public int PostCount { get; set; }
    public int MutualFriendCount { get; set; }
    public string FriendshipStatus { get; set; } = "None";

    public List<EducationDto> Education { get; set; } = [];
    public List<ExperienceDto> Experience { get; set; } = [];
    public List<SocialLinkDto> SocialLinks { get; set; } = [];

    // Flat alias the Angular client reads/sends.
    public DateTime? DateOfBirth => Birthday;
}
