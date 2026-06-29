using AutoMapper;
using LinkUp.Modules.Identity.DTOs;
using LinkUp.Modules.Identity.Entities;

namespace LinkUp.Modules.Identity.Mappings;

public class IdentityMappingProfile : Profile
{
    public IdentityMappingProfile()
    {
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FullName));
    }
}
