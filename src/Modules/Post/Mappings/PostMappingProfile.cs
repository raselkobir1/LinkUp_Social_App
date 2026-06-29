using AutoMapper;
using LinkUp.Modules.Post.DTOs;
using LinkUp.Modules.Post.Entities;
using PostEntity = LinkUp.Modules.Post.Entities.Post;

namespace LinkUp.Modules.Post.Mappings;

public class PostMappingProfile : Profile
{
    public PostMappingProfile()
    {
        CreateMap<PostImage, PostImageDto>();

        CreateMap<PostVideo, PostVideoDto>();

        CreateMap<PostEntity, PostDto>()
            .ForMember(d => d.Images, o => o.MapFrom(s => s.Images.OrderBy(i => i.DisplayOrder).ToList()))
            .ForMember(d => d.Video, o => o.MapFrom(s => s.Video))
            .ForMember(d => d.Author, o => o.Ignore())
            .ForMember(d => d.OriginalPost, o => o.Ignore());
    }
}
