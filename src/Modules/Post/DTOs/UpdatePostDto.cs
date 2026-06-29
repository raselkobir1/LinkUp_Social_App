using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Post.DTOs;

public class UpdatePostDto
{
    public string? Content { get; set; }
    public PostVisibility Visibility { get; set; }
}
