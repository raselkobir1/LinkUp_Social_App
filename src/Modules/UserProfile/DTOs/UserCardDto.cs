namespace LinkUp.Modules.UserProfile.DTOs;

public class UserCardDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }
}
