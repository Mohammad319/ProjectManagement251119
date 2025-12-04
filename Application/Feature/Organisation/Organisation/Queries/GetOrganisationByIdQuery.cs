using Application.Interfaces;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.Organisation.Queries
{
    public sealed record GetOrganisationByIdQuery(int Id) : IRequest<OrganisationDetailsDTO>;
    public class GetCompanyByIdQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetOrganisationByIdQuery, OrganisationDetailsDTO>
    {
        public async Task<OrganisationDetailsDTO> Handle(GetOrganisationByIdQuery query, CancellationToken cancellationToken)
        {
            OrganisationEntity x = await context.Organisation.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id);
            OrganisationDetailsDTO post = new()
            {
                Address = x.Data?.Address,
                VerificationDate = x.Data?.VerificationDate,
                Contacts = x.Data?.Contacts,
                InvoiceVerificationDate = x.Data?.InvoiceVerificationDate,
                Category = x.Category?.Category?.Name,
                SubCategory = x.Category?.Name,
                Email = x.Data?.Email,
                EnvironmentalSystems = x.Data?.EnvironmentalSystems,
                IDNumber = x.Data?.IDNumber,
                Name = x.Name,
                Mobile = x.Data?.Mobile,
                Notes = x.Data?.Notes,
                NumberOfWorkersCards = x.Data?.NumberOfWorkersCards,
                Phone = x.Data?.Phone,
                PIDNumber = x.Data?.PIDNumber,
                Type = x.OrganisationType?.Name,
                QualitySystems = x.Data?.QualitySystems,
                Rating = x.Data?.Rating,
                SocialLaborAgreement = x.Data?.SocialLaborAgreement,
                Status = x.Data?.Status,
                URL = x.Data?.URL,
            };
            return post;
        }
    }
}
