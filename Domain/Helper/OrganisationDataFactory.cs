using ProjectManagement.Shared.DTO.Organisation;

namespace Domain.Helper.Organisation
{
    public static class OrganisationDataFactory
    {
        public static OrganisationData From(PostOrganisationDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new OrganisationData
            {
                Address = dto.Address ?? [],
                VerificationDate = dto.VerificationDate,
                InvoiceVerificationDate = dto.InvoiceVerificationDate,
                Contacts = dto.Contacts ?? [],
                Email = Normalize(dto.Email),
                EnvironmentalSystems = dto.EnvironmentalSystems,
                IDNumber = Normalize(dto.IDNumber),
                Mobile = Normalize(dto.Mobile),
                Notes = (dto.Notes ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
                NumberOfWorkersCards = dto.NumberOfWorkersCards,
                Phone = Normalize(dto.Phone),
                PIDNumber = Normalize(dto.PIDNumber),
                QualitySystems = dto.QualitySystems,
                Rating = dto.Rating,
                SocialLaborAgreement = dto.SocialLaborAgreement,
                Status = Normalize(dto.Status),
                URL = Normalize(dto.URL)
            };
        }

        private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
