using AutoMapper;
using LinkUp.Modules.Comment.DTOs;
using CommentEntity = LinkUp.Modules.Comment.Entities.Comment;

namespace LinkUp.Modules.Comment.Mappings;

public class CommentMappingProfile : Profile
{
    public CommentMappingProfile()
    {
        CreateMap<CommentEntity, CommentDto>()
            .ForMember(d => d.Author, o => o.Ignore())
            .ForMember(d => d.IsLikedByCurrentUser, o => o.Ignore())
            .ForMember(d => d.Replies, o => o.Ignore());
    }
}
