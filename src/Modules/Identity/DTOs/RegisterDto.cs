namespace LinkUp.Modules.Identity.DTOs;

public record RegisterDto(
    string FirstName,
    string LastName,
    string Email,
    string UserName,
    string Password,
    string ConfirmPassword
);
