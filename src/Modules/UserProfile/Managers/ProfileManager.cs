using AutoMapper;
using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.Modules.Identity.Entities;
using LinkUp.Modules.UserProfile.Configuration;
using LinkUp.Modules.UserProfile.DTOs;
using LinkUp.Modules.UserProfile.Entities;
using LinkUp.Modules.UserProfile.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.UserProfile.Managers;

public class ProfileManager(ProfileDbContext db, IMapper mapper, UserManager<ApplicationUser> userManager) : IProfileManager
{
    private async Task<Entities.UserProfile> EnsureProfileAsync(Guid userId, CancellationToken ct)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted, ct);
        if (profile is null)
        {
            profile = new Entities.UserProfile { UserId = userId };
            db.Profiles.Add(profile);
        }
        return profile;
    }

    private async Task SyncUserAvatarAsync(Guid userId, string? pictureUrl, string? coverUrl)
    {
        // Friends/posts/chat DTOs read the avatar from ApplicationUser, so keep it in sync.
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;
        if (pictureUrl is not null) user.ProfilePictureUrl = pictureUrl;
        if (coverUrl is not null) user.CoverPhotoUrl = coverUrl;
        await userManager.UpdateAsync(user);
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, Guid viewerId, CancellationToken ct = default)
    {
        var profile = await db.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted, ct);

        // Profiles are created lazily on first access — every authenticated user has one.
        if (profile is null)
        {
            profile = new Entities.UserProfile { UserId = userId };
            db.Profiles.Add(profile);
            await db.SaveChangesAsync(ct);
        }

        return mapper.Map<UserProfileDto>(profile);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default)
    {
        var profile = await db.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted, ct);

        if (profile is null)
        {
            profile = new Entities.UserProfile { UserId = userId };
            db.Profiles.Add(profile);
        }

        profile.Bio = dto.Bio;
        profile.Gender = dto.Gender;
        profile.Birthday = dto.Birthday;
        profile.Location = dto.Location;
        profile.Website = dto.Website;
        profile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return mapper.Map<UserProfileDto>(profile);
    }

    public async Task<UserProfileDto> GetOrCreateProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await db.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted, ct);

        if (profile is null)
        {
            profile = new Entities.UserProfile { UserId = userId };
            db.Profiles.Add(profile);
            await db.SaveChangesAsync(ct);
        }

        return mapper.Map<UserProfileDto>(profile);
    }

    public async Task<UserProfileDto> UploadProfilePictureAsync(Guid userId, string url, CancellationToken ct = default)
    {
        var profile = await EnsureProfileAsync(userId, ct);
        profile.ProfilePictureUrl = url;
        profile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await SyncUserAvatarAsync(userId, url, null);
        return mapper.Map<UserProfileDto>(profile);
    }

    public async Task<UserProfileDto> UploadCoverPhotoAsync(Guid userId, string url, CancellationToken ct = default)
    {
        var profile = await EnsureProfileAsync(userId, ct);
        profile.CoverPhotoUrl = url;
        profile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await SyncUserAvatarAsync(userId, null, url);
        return mapper.Map<UserProfileDto>(profile);
    }

    // --- Education ---

    public async Task<List<EducationDto>> GetEducationsAsync(Guid userId, CancellationToken ct = default)
    {
        var educations = await db.UserEducations
            .AsNoTracking()
            .Where(e => e.UserId == userId && !e.IsDeleted)
            .OrderByDescending(e => e.StartYear)
            .ToListAsync(ct);

        return mapper.Map<List<EducationDto>>(educations);
    }

    public async Task<EducationDto> AddEducationAsync(Guid userId, CreateEducationDto dto, CancellationToken ct = default)
    {
        var education = mapper.Map<UserEducation>(dto);
        education.UserId = userId;

        db.UserEducations.Add(education);
        await db.SaveChangesAsync(ct);

        return mapper.Map<EducationDto>(education);
    }

    public async Task<EducationDto> UpdateEducationAsync(Guid userId, Guid id, UpdateEducationDto dto, CancellationToken ct = default)
    {
        var education = await db.UserEducations
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId && !e.IsDeleted, ct)
            ?? throw new NotFoundException("Education", id);

        education.School = dto.School;
        education.Degree = dto.Degree;
        education.FieldOfStudy = dto.FieldOfStudy;
        education.StartYear = dto.StartYear;
        education.EndYear = dto.EndYear;
        education.IsCurrent = dto.IsCurrent;
        education.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return mapper.Map<EducationDto>(education);
    }

    public async Task DeleteEducationAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var education = await db.UserEducations
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId && !e.IsDeleted, ct)
            ?? throw new NotFoundException("Education", id);

        education.IsDeleted = true;
        education.DeletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    // --- Experience ---

    public async Task<List<ExperienceDto>> GetExperiencesAsync(Guid userId, CancellationToken ct = default)
    {
        var experiences = await db.UserExperiences
            .AsNoTracking()
            .Where(e => e.UserId == userId && !e.IsDeleted)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(ct);

        return mapper.Map<List<ExperienceDto>>(experiences);
    }

    public async Task<ExperienceDto> AddExperienceAsync(Guid userId, CreateExperienceDto dto, CancellationToken ct = default)
    {
        var experience = mapper.Map<UserExperience>(dto);
        experience.UserId = userId;

        db.UserExperiences.Add(experience);
        await db.SaveChangesAsync(ct);

        return mapper.Map<ExperienceDto>(experience);
    }

    public async Task<ExperienceDto> UpdateExperienceAsync(Guid userId, Guid id, UpdateExperienceDto dto, CancellationToken ct = default)
    {
        var experience = await db.UserExperiences
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId && !e.IsDeleted, ct)
            ?? throw new NotFoundException("Experience", id);

        experience.Company = dto.Company;
        experience.Position = dto.Position;
        experience.StartDate = dto.StartDate;
        experience.EndDate = dto.EndDate;
        experience.IsCurrent = dto.IsCurrent;
        experience.Description = dto.Description;
        experience.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return mapper.Map<ExperienceDto>(experience);
    }

    public async Task DeleteExperienceAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var experience = await db.UserExperiences
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId && !e.IsDeleted, ct)
            ?? throw new NotFoundException("Experience", id);

        experience.IsDeleted = true;
        experience.DeletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    // --- Social Links ---

    public async Task<List<SocialLinkDto>> GetSocialLinksAsync(Guid userId, CancellationToken ct = default)
    {
        var links = await db.SocialLinks
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .ToListAsync(ct);

        return mapper.Map<List<SocialLinkDto>>(links);
    }

    public async Task<SocialLinkDto> AddSocialLinkAsync(Guid userId, CreateSocialLinkDto dto, CancellationToken ct = default)
    {
        var link = mapper.Map<SocialLink>(dto);
        link.UserId = userId;

        db.SocialLinks.Add(link);
        await db.SaveChangesAsync(ct);

        return mapper.Map<SocialLinkDto>(link);
    }

    public async Task DeleteSocialLinkAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var link = await db.SocialLinks
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId && !s.IsDeleted, ct)
            ?? throw new NotFoundException("SocialLink", id);

        link.IsDeleted = true;
        link.DeletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    // --- Privacy Settings ---

    public async Task<PrivacySettingsDto> GetPrivacySettingsAsync(Guid userId, CancellationToken ct = default)
    {
        var settings = await db.PrivacySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (settings is null)
        {
            settings = new PrivacySettings { UserId = userId };
            db.PrivacySettings.Add(settings);
            await db.SaveChangesAsync(ct);
        }

        return mapper.Map<PrivacySettingsDto>(settings);
    }

    public async Task<PrivacySettingsDto> UpdatePrivacySettingsAsync(Guid userId, UpdatePrivacySettingsDto dto, CancellationToken ct = default)
    {
        var settings = await db.PrivacySettings
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (settings is null)
        {
            settings = new PrivacySettings { UserId = userId };
            db.PrivacySettings.Add(settings);
        }

        settings.ProfileVisibility = dto.ProfileVisibility;
        settings.FriendListVisibility = dto.FriendListVisibility;
        settings.PostDefaultVisibility = dto.PostDefaultVisibility;

        await db.SaveChangesAsync(ct);
        return mapper.Map<PrivacySettingsDto>(settings);
    }
}
