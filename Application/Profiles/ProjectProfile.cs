using AutoMapper;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Profiles
{
    public class ProjectProfile : Profile
    {
        public ProjectProfile()
        {
            CreateMap<ProjectEntity, PostProjectDTO>().ReverseMap();
            CreateMap<TypeEntity, PostTypeDTO>().ReverseMap();
            CreateMap<ProcurementMethodEntity, PostProcurementMethodsDTO>().ReverseMap();
            CreateMap<ContractEntity, PostContractDTO>().ReverseMap();
            CreateMap<CompensationEntity, PostCompensationDTO>().ReverseMap();
        }
    }
}