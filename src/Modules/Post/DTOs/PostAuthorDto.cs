namespace LinkUp.Modules.Post.DTOs;

public class PostAuthorDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string UserName { get; set; } = string.Empty;
}
