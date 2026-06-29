using LinkUp.Modules.Identity.DTOs;

namespace LinkUp.Modules.Identity.Interfaces;

public interface IAuthManager
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default);
    Task VerifyEmailAsync(string userId, string token, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default);
}
