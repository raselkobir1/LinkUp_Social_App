using AutoMapper;
using LinkUp.Modules.VideoCall.DTOs;
using LinkUp.Modules.VideoCall.Entities;

namespace LinkUp.Modules.VideoCall.Mappings;

public class VideoCallMappingProfile : Profile
{
    public VideoCallMappingProfile()
    {
        CreateMap<Call, CallDto>()
            .ForMember(d => d.Participants, o => o.Ignore());

        CreateMap<Call, CallHistoryDto>()
            .ForMember(d => d.Participants, o => o.Ignore())
            .ForMember(d => d.IsIncoming, o => o.Ignore());

        CreateMap<CallParticipant, CallParticipantDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(_ => string.Empty))
            .ForMember(d => d.ProfilePictureUrl, o => o.MapFrom(_ => (string?)null));
    }
}
