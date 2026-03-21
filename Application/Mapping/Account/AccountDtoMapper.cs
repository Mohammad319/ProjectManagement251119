using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Account;
using System.Linq.Expressions;

namespace Application.Mapping.Account
{
    public static class AccountDtoMapper
    {
        public static AccountData ToData(this PostAccountDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            return dto.Data.Clone();
        }

        public static Expression<Func<AccountEntity, AccountManageDTO>> ProjectManageDto()
            => x => new AccountManageDTO
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsVisible = x.IsVisible,
                Metadata = x.Metadata
            };
    }
}
