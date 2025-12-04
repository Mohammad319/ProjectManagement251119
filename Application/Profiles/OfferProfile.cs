using AutoMapper;
using Domain.Entities.Calculation;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.DTO.Offer;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Profiles
{
    class OfferProfile : Profile
    {
        public OfferProfile()
        {
            CreateMap<OrganisationCategoryEntity, PostOrganisationCategoryDTO>().ReverseMap();
            CreateMap<OrganisationEntity, PostOrganisationDTO>().ReverseMap();
            CreateMap<OfferEntity, PostOfferDTO>().ReverseMap();
        }
    }
}
