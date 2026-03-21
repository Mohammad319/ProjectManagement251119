using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.DTO.App;

namespace Domain.Helper
{
    internal static class MetadataCloneHelper
    {
        public static List<string> CloneStrings(IEnumerable<string>? values)
            => values?.ToList() ?? [];

        public static List<AddressDTO> CloneAddresses(IEnumerable<AddressDTO>? values)
            => values?.Select(CloneAddress).ToList() ?? [];

        public static List<UnderContactOrganisationBase> CloneContacts(IEnumerable<UnderContactOrganisationBase>? values)
            => values?.Select(CloneContact).ToList() ?? [];

        public static AddressDTO CloneAddress(AddressDTO value)
            => new()
            {
                Street = value.Street ?? string.Empty,
                ZIPCode = value.ZIPCode ?? string.Empty,
                Nr = value.Nr ?? string.Empty,
                City = value.City ?? string.Empty,
                Region = value.Region ?? string.Empty,
                Country = value.Country ?? string.Empty
            };

        public static UnderContactOrganisationBase CloneContact(UnderContactOrganisationBase value)
            => new()
            {
                CommentIsVisible = value.CommentIsVisible,
                FirstName = value.FirstName ?? string.Empty,
                LastName = value.LastName ?? string.Empty,
                Email = value.Email ?? string.Empty,
                Telefone = value.Telefone ?? string.Empty,
                Mobile = value.Mobile ?? string.Empty,
                Department = value.Department ?? string.Empty,
                Note = value.Note ?? string.Empty,
                Status = value.Status
            };
    }
}
