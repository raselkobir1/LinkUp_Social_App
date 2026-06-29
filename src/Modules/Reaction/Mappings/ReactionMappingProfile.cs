using AutoMapper;
using LinkUp.Modules.Reaction.DTOs;

namespace LinkUp.Modules.Reaction.Mappings;

public class ReactionMappingProfile : Profile
{
    public ReactionMappingProfile()
    {
        CreateMap<Entities.Reaction, ReactorDto>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
            .ForMember(d => d.ReactionType, o => o.MapFrom(s => s.Type))
            .ForMember(d => d.FullName, o => o.Ignore())
            .ForMember(d => d.ProfilePictureUrl, o => o.Ignore());
    }
}
