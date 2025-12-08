using Application.Interfaces;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.Organisation.Queries
{
    public sealed record GetOrganisationToPostQuery(int Id) : IRequest<PostOrganisationDTO>;
    public class GetOrganisationToPostQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetOrganisationToPostQuery, PostOrganisationDTO>
    {
        public async Task<PostOrganisationDTO> Handle(GetOrganisationToPostQuery query, CancellationToken cancellationToken)
        {
            OrganisationEntity x = await context.Organisation.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id);
            PostOrganisationDTO post = new()
            {
                Address = x.Metadata?.Address,
                VerificationDate = x.Metadata?.VerificationDate,
                Contacts = x.Metadata?.Contacts,
                InvoiceVerificationDate = x.Metadata?.InvoiceVerificationDate,
                CategoryId = x.OrganisationCategoryId,
                IsVisible = x.IsVisible,
                Email = x.Metadata?.Email,
                EnvironmentalSystems = x.Metadata.EnvironmentalSystems,
                IDNumber = x.Metadata?.IDNumber,
                Name = x.Name,
                Mobile = x.Metadata?.Mobile,
                Notes = x.Metadata?.Notes,
                NumberOfWorkersCards = x.Metadata?.NumberOfWorkersCards,
                Phone = x.Metadata?.Phone,
                PIDNumber = x.Metadata?.PIDNumber,
                OrganisationTypeID = x.OrganisationTypeId,
                QualitySystems = x.Metadata.QualitySystems,
                Rating = x.Metadata?.Rating,
                SocialLaborAgreement = x.Metadata.SocialLaborAgreement,
                Status = x.Metadata?.Status,
                URL = x.Metadata?.URL,
            };
            return post;
        }
    }
}
