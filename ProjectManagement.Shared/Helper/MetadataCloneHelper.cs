using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.DTO.App;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Shared.Helper
{
    public static class MetadataCloneHelper
    {
        public static string CopyText(string? value)
            => value ?? string.Empty;

        public static List<string> CloneStrings(IEnumerable<string>? values)
            => values?.Select(CopyText).ToList() ?? [];

        public static List<AddressDTO> CloneAddresses(IEnumerable<AddressDTO>? values)
            => values?.Select(CloneAddress).ToList() ?? [];

        public static AddressDTO CloneAddress(AddressDTO? value)
            => new()
            {
                Street = CopyText(value?.Street),
                ZIPCode = CopyText(value?.ZIPCode),
                Nr = CopyText(value?.Nr),
                City = CopyText(value?.City),
                Region = CopyText(value?.Region),
                Country = CopyText(value?.Country)
            };

        public static List<UnderContactOrganisationBase> CloneContacts(IEnumerable<UnderContactOrganisationBase>? values)
            => values?.Select(CloneContact).ToList() ?? [];

        public static UnderContactOrganisationBase CloneContact(UnderContactOrganisationBase? value)
            => new()
            {
                FirstName = CopyText(value?.FirstName),
                LastName = CopyText(value?.LastName),
                Email = CopyText(value?.Email),
                Telefone = CopyText(value?.Telefone),
                Mobile = CopyText(value?.Mobile),
                Department = CopyText(value?.Department),
                Note = CopyText(value?.Note),
                Status = value?.Status ?? default,
                CommentIsVisible = value?.CommentIsVisible ?? false
            };
    }
}
