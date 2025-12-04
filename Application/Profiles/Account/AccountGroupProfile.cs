using Application.Feature.Account.Commands;
using AutoMapper;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Account;

namespace Application.Profiles.Account
{
    class AccountGroupProfile : Profile
    {
        public AccountGroupProfile()
        {
            CreateMap<AccountEntity, PostAccountDTO>().ReverseMap();
            CreateMap<AccountGroupEntity, PostAccountGroupDTO>().ReverseMap();
        }
    }
}
