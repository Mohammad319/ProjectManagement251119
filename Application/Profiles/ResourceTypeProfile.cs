using AutoMapper;
using Domain.Entities.Calculation;
using Domain.Entities.ResourceType;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Profiles
{
    public class ResourceTypeProfile : Profile
    {
        public ResourceTypeProfile()
        {
            CreateMap<ResourceTypeEntity, PostResourceTypeDTO>().ReverseMap();


            CreateMap<StatusResourcesEntity, PostResourceStatusDTO>().ReverseMap();
        }
    }
}
