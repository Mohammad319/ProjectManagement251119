using ProjectManagement.Shared.DTO.Organisation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Helper.Organisation
{
    public static class OrganisationDataFactory
    {
        public static OrganisationData From(PostOrganisationDTO dto)
        {
            return new OrganisationData
            {
                Address = dto.Address,
                VerificationDate = dto.VerificationDate,
                InvoiceVerificationDate = dto.InvoiceVerificationDate,
                Contacts = dto.Contacts,
                Email = dto.Email,
                EnvironmentalSystems = dto.EnvironmentalSystems,
                IDNumber = dto.IDNumber,
                Mobile = dto.Mobile,
                Notes = dto.Notes,
                NumberOfWorkersCards = dto.NumberOfWorkersCards,
                Phone = dto.Phone,
                PIDNumber = dto.PIDNumber,
                QualitySystems = dto.QualitySystems,
                Rating = dto.Rating,
                SocialLaborAgreement = dto.SocialLaborAgreement,
                Status = dto.Status,
                URL = dto.URL
            };
        }
    }

}
