using AutoMapper;
using LinkUp.Modules.Friend.DTOs;
using LinkUp.Modules.Friend.Entities;

namespace LinkUp.Modules.Friend.Mappings;

public class FriendMappingProfile : Profile
{
    public FriendMappingProfile()
    {
        CreateMap<FriendRequest, FriendRequestDto>()
            .ForMember(d => d.SentAt, o => o.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.Sender, o => o.MapFrom(s => new UserCardDto
            {
                Id = s.SenderId,
                FullName = s.SenderName,
                ProfilePictureUrl = s.SenderProfilePictureUrl
            }))
            .ForMember(d => d.Receiver, o => o.MapFrom(s => new UserCardDto
            {
                Id = s.ReceiverId,
                FullName = s.ReceiverName,
                ProfilePictureUrl = s.ReceiverProfilePictureUrl
            }));

        CreateMap<Friendship, FriendDto>()
            .ForMember(d => d.FriendSince, o => o.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.FriendInfo, o => o.MapFrom(s => new UserCardDto
            {
                Id = s.FriendId,
                FullName = s.FriendName,
                ProfilePictureUrl = s.FriendProfilePictureUrl
            }));
    }
}
