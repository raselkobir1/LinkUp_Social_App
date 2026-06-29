using AutoMapper;
using LinkUp.Modules.Chat.DTOs;
using LinkUp.Modules.Chat.Entities;

namespace LinkUp.Modules.Chat.Mappings;

public class ChatMappingProfile : Profile
{
    public ChatMappingProfile()
    {
        CreateMap<Entities.Chat, ChatListItemDto>()
            .ForMember(d => d.GroupName, o => o.Ignore())
            .ForMember(d => d.GroupPhotoUrl, o => o.Ignore())
            .ForMember(d => d.Participants, o => o.Ignore())
            .ForMember(d => d.LastMessage, o => o.Ignore())
            .ForMember(d => d.UnreadCount, o => o.Ignore());

        CreateMap<Message, MessageDto>()
            .ForMember(d => d.Sender, o => o.Ignore())
            .ForMember(d => d.ReplyTo, o => o.Ignore())
            .ForMember(d => d.Attachments, o => o.Ignore())
            .ForMember(d => d.Reads, o => o.Ignore());

        CreateMap<MessageAttachment, MessageAttachmentDto>();

        CreateMap<MessageRead, MessageReadDto>();

        CreateMap<ChatParticipant, ChatParticipantDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(_ => string.Empty))
            .ForMember(d => d.ProfilePictureUrl, o => o.MapFrom(_ => (string?)null));

        CreateMap<GroupChat, GroupChatDetailsDto>()
            .ForMember(d => d.CreatedById, o => o.MapFrom(src => src.CreatedById ?? Guid.Empty))
            .ForMember(d => d.Participants, o => o.Ignore());
    }
}
