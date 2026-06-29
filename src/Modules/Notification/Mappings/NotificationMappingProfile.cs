using AutoMapper;
using LinkUp.Modules.Notification.DTOs;
using NotificationEntity = LinkUp.Modules.Notification.Entities.Notification;

namespace LinkUp.Modules.Notification.Mappings;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<NotificationEntity, NotificationDto>()
            .ForMember(d => d.Sender, o => o.Ignore());
    }
}
