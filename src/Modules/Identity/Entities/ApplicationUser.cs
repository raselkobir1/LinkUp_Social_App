using Microsoft.AspNetCore.Identity;

namespace LinkUp.Modules.Identity.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSuspended { get; set; } = false;
    public string? SuspensionReason { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool IsOnline { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
