using Application.Feature.Calculation.CalcShare.Commands;
using Application.Feature.Calculation.Calculation.Commands;
using AutoMapper;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Profiles.Calculation
{
    class CalculationProfile : Profile
    {
        public CalculationProfile()
        {
            CreateMap<CalculationEntity, PostCalculationDTO>().ReverseMap();
            CreateMap<CalculationEntity, PostCalculationDTO>().ReverseMap();

            CreateMap<OpportunityEntity, PostOpportunityDTO>().ReverseMap();

            CreateMap<ShareCalcEntity, PostShareCalcDTO>().ReverseMap();
            CreateMap<ShareCalcEntity, UpdateShareCalcDTO>().ReverseMap();

        }
    }
}
