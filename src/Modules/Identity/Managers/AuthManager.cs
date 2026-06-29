using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.Modules.Identity.Configuration;
using LinkUp.Modules.Identity.DTOs;
using LinkUp.Modules.Identity.Entities;
using LinkUp.Modules.Identity.Interfaces;
using LinkUp.Modules.Identity.Services;
using LinkUp.SharedKernel.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LinkUp.Modules.Identity.Managers;

public class AuthManager(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtService jwtService,
    IdentityDbContext dbContext,
    IConfiguration configuration) : IAuthManager
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        if (await userManager.FindByEmailAsync(dto.Email) is not null)
            throw new ConflictException("Email is already registered.");

        if (await userManager.FindByNameAsync(dto.UserName) is not null)
            throw new ConflictException("Username is already taken.");

        var user = new ApplicationUser
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            UserName = dto.UserName,
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.Select(e => e.Description));

        await userManager.AddToRoleAsync(user, AppConstants.Roles.User);

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(dto.Email)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (user.IsSuspended)
            throw new ForbiddenException($"Account suspended. Reason: {user.SuspensionReason}");

        if (!user.IsActive)
            throw new ForbiddenException("Account is inactive.");

        var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                throw new ForbiddenException("Account is locked. Try again later.");
            throw new UnauthorizedException("Invalid email or password.");
        }

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken ct = default)
    {
        var principal = jwtService.GetPrincipalFromExpiredToken(dto.AccessToken)
            ?? throw new UnauthorizedException("Invalid access token.");

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedException("Invalid token claims.");

        var userId = Guid.Parse(userIdClaim);

        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken && rt.UserId == userId, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (!storedToken.IsActive)
            throw new UnauthorizedException("Refresh token is expired or revoked.");

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User", userId);

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        return await BuildAuthResponseAsync(user, ct);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, ct);

        if (token is not null)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null) return;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        // TODO: send email with token
        // emailService.SendPasswordResetEmailAsync(user.Email, token)
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            throw new ValidationException("Passwords do not match.");

        var user = await userManager.FindByEmailAsync(dto.Email)
            ?? throw new NotFoundException("User not found.");

        var result = await userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.Select(e => e.Description));
    }

    public async Task VerifyEmailAsync(string userId, string token, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.Select(e => e.Description));
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            throw new ValidationException("Passwords do not match.");

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User", userId);

        var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.Select(e => e.Description));
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(ApplicationUser user, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = jwtService.GenerateAccessToken(user, roles);
        var refreshTokenValue = jwtService.GenerateRefreshToken();
        var expireDays = int.Parse(configuration["Jwt:RefreshTokenExpireDays"] ?? "30");

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(expireDays)
        };

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(ct);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = jwtService.GetAccessTokenExpiry(),
            User = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email!,
                UserName = user.UserName!,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CoverPhotoUrl = user.CoverPhotoUrl,
                Roles = roles
            }
        };
    }
}
