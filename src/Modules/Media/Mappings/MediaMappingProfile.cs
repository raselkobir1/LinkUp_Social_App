using AutoMapper;
using LinkUp.Modules.Media.DTOs;
using LinkUp.Modules.Media.Entities;

namespace LinkUp.Modules.Media.Mappings;

public class MediaMappingProfile : Profile
{
    public MediaMappingProfile()
    {
        CreateMap<MediaFile, MediaFileDto>();
        CreateMap<MediaFile, MediaUploadResultDto>();
    }
}
