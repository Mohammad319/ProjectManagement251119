using Application.Feature.Project.Status.Commands;
using AutoMapper;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Profiles
{
    public class StatusProfille : Profile
    {
        public StatusProfille()
        {
            CreateMap<StatusEntity, PostStatusDTO>().ReverseMap();
        }
    }
}
