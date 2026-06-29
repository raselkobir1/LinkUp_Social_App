namespace LinkUp.Modules.Identity.DTOs;

public record ForgotPasswordDto(string Email);

public record ResetPasswordDto(string Email, string Token, string NewPassword, string ConfirmPassword);
