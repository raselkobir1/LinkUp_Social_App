using AutoMapper;
using LinkUp.Modules.UserProfile.DTOs;
using LinkUp.Modules.UserProfile.Entities;

namespace LinkUp.Modules.UserProfile.Mappings;

public class ProfileMappingProfile : Profile
{
    public ProfileMappingProfile()
    {
        CreateMap<Entities.UserProfile, UserProfileDto>();
        CreateMap<UpdateProfileDto, Entities.UserProfile>();

        CreateMap<UserEducation, EducationDto>();
        CreateMap<CreateEducationDto, UserEducation>();
        CreateMap<UpdateEducationDto, UserEducation>();

        CreateMap<UserExperience, ExperienceDto>();
        CreateMap<CreateExperienceDto, UserExperience>();
        CreateMap<UpdateExperienceDto, UserExperience>();

        CreateMap<SocialLink, SocialLinkDto>();
        CreateMap<CreateSocialLinkDto, SocialLink>();

        CreateMap<PrivacySettings, PrivacySettingsDto>();
        CreateMap<UpdatePrivacySettingsDto, PrivacySettings>();
    }
}
