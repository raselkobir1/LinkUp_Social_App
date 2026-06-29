using Asp.Versioning;
using LinkUp.BuildingBlocks.Common.Controllers;
using LinkUp.Modules.Identity.DTOs;
using LinkUp.Modules.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.Modules.Identity.Controllers;

[ApiVersion("1.0")]
public class AuthController(IAuthManager authManager) : BaseApiController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        var result = await authManager.RegisterAsync(dto, ct);
        return ApiCreated(result, "Registration successful. Please verify your email.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await authManager.LoginAsync(dto, ct);
        return ApiOk(result, "Login successful.");
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto, CancellationToken ct)
    {
        var result = await authManager.RefreshTokenAsync(dto, ct);
        return ApiOk(result, "Token refreshed.");
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutDto dto, CancellationToken ct)
    {
        await authManager.LogoutAsync(dto.RefreshToken, ct);
        return ApiOk<object>(null!, "Logged out successfully.");
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken ct)
    {
        await authManager.ForgotPasswordAsync(dto, ct);
        return ApiOk<object>(null!, "If the email exists, a reset link has been sent.");
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        await authManager.ResetPasswordAsync(dto, ct);
        return ApiOk<object>(null!, "Password reset successful.");
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string userId, [FromQuery] string token, CancellationToken ct)
    {
        await authManager.VerifyEmailAsync(userId, token, ct);
        return ApiOk<object>(null!, "Email verified successfully.");
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        await authManager.ChangePasswordAsync(CurrentUserId, dto, ct);
        return ApiOk<object>(null!, "Password changed successfully.");
    }
}
