using LinkUp.Modules.UserProfile.DTOs;

namespace LinkUp.Modules.UserProfile.Interfaces;

public interface IProfileManager
{
    Task<UserProfileDto> GetProfileAsync(Guid userId, Guid viewerId, CancellationToken ct = default);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default);
    Task<UserProfileDto> GetOrCreateProfileAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfileDto> UploadProfilePictureAsync(Guid userId, string url, CancellationToken ct = default);
    Task<UserProfileDto> UploadCoverPhotoAsync(Guid userId, string url, CancellationToken ct = default);

    Task<List<EducationDto>> GetEducationsAsync(Guid userId, CancellationToken ct = default);
    Task<EducationDto> AddEducationAsync(Guid userId, CreateEducationDto dto, CancellationToken ct = default);
    Task<EducationDto> UpdateEducationAsync(Guid userId, Guid id, UpdateEducationDto dto, CancellationToken ct = default);
    Task DeleteEducationAsync(Guid userId, Guid id, CancellationToken ct = default);

    Task<List<ExperienceDto>> GetExperiencesAsync(Guid userId, CancellationToken ct = default);
    Task<ExperienceDto> AddExperienceAsync(Guid userId, CreateExperienceDto dto, CancellationToken ct = default);
    Task<ExperienceDto> UpdateExperienceAsync(Guid userId, Guid id, UpdateExperienceDto dto, CancellationToken ct = default);
    Task DeleteExperienceAsync(Guid userId, Guid id, CancellationToken ct = default);

    Task<List<SocialLinkDto>> GetSocialLinksAsync(Guid userId, CancellationToken ct = default);
    Task<SocialLinkDto> AddSocialLinkAsync(Guid userId, CreateSocialLinkDto dto, CancellationToken ct = default);
    Task DeleteSocialLinkAsync(Guid userId, Guid id, CancellationToken ct = default);

    Task<PrivacySettingsDto> GetPrivacySettingsAsync(Guid userId, CancellationToken ct = default);
    Task<PrivacySettingsDto> UpdatePrivacySettingsAsync(Guid userId, UpdatePrivacySettingsDto dto, CancellationToken ct = default);
}
