using AutoMapper;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Profiles.Calculation
{
    class TaskProfile : Profile
    {
        public TaskProfile()
        {
            CreateMap<TaskEntity, TaskPostDTO>().ReverseMap();

            CreateMap<TaskStatusEntity, PostTaskStatusDTO>().ReverseMap();
        }
    }
    class ResourceProfile : Profile
    {
        public ResourceProfile()
        {
            CreateMap<ResourceEntity, ResourcePostDTO>().ReverseMap();
            CreateMap<ResourceEntity, ResourceListDTO>().ReverseMap();
        }
    }
}
