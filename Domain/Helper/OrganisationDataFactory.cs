using ProjectManagement.Shared.DTO.Organisation;

namespace Domain.Helper.Organisation
{
    public static class OrganisationDataFactory
    {
        public static OrganisationData From(PostOrganisationDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return Clone(dto.Data);
        }

        public static OrganisationData Clone(OrganisationData? data)
        {
            data ??= new OrganisationData();

            return new OrganisationData
            {
                Address = Domain.Helper.MetadataCloneHelper.CloneAddresses(data.Address),
                VerificationDate = data.VerificationDate,
                InvoiceVerificationDate = data.InvoiceVerificationDate,
                Contacts = Domain.Helper.MetadataCloneHelper.CloneContacts(data.Contacts),
                Email = Normalize(data.Email),
                EnvironmentalSystems = data.EnvironmentalSystems,
                IDNumber = Normalize(data.IDNumber),
                Mobile = Normalize(data.Mobile),
                Notes = (data.Notes ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
                NumberOfWorkersCards = data.NumberOfWorkersCards,
                Phone = Normalize(data.Phone),
                PIDNumber = Normalize(data.PIDNumber),
                QualitySystems = data.QualitySystems,
                Rating = data.Rating,
                SocialLaborAgreement = data.SocialLaborAgreement,
                Status = Normalize(data.Status),
                OrganisationType = Normalize(data.OrganisationType),
                URL = Normalize(data.URL),
                WarningReason = Normalize(data.WarningReason)
            };
        }

        private static string Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
