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
                Address = x.Metadata?.Address,
                VerificationDate = x.Metadata?.VerificationDate,
                Contacts = x.Metadata?.Contacts,
                InvoiceVerificationDate = x.Metadata?.InvoiceVerificationDate,
                Category = x.OrganisationCategory?.ParentCategory?.Name,
                SubCategory = x.OrganisationCategory?.Name,
                Email = x.Metadata?.Email,
                EnvironmentalSystems = x.Metadata?.EnvironmentalSystems,
                IDNumber = x.Metadata?.IDNumber,
                Name = x.Name,
                Mobile = x.Metadata?.Mobile,
                Notes = x.Metadata?.Notes,
                NumberOfWorkersCards = x.Metadata?.NumberOfWorkersCards,
                Phone = x.Metadata?.Phone,
                PIDNumber = x.Metadata?.PIDNumber,
                Type = x.OrganisationType?.Name,
                QualitySystems = x.Metadata?.QualitySystems,
                Rating = x.Metadata?.Rating,
                SocialLaborAgreement = x.Metadata?.SocialLaborAgreement,
                Status = x.Metadata?.Status,
                URL = x.Metadata?.URL,
            };
            return post;
        }
    }
}
