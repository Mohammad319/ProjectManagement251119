using AutoMapper;
using Domain.Entities.ResourceType;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Profiles
{
    public class ResourceSortProfile : Profile
    {
        public ResourceSortProfile()
        {
            CreateMap<ResourceSortEntity, PostResourceSortDTO>().ReverseMap();
        }
    }
}
