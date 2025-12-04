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
                Address = x.Data?.Address,
                VerificationDate = x.Data?.VerificationDate,
                Contacts = x.Data?.Contacts,
                InvoiceVerificationDate = x.Data?.InvoiceVerificationDate,
                CategoryId = x.CategoryId,
                IsVisible = x.IsVisible,
                Email = x.Data?.Email,
                EnvironmentalSystems = x.Data.EnvironmentalSystems,
                IDNumber = x.Data?.IDNumber,
                Name = x.Name,
                Mobile = x.Data?.Mobile,
                Notes = x.Data?.Notes,
                NumberOfWorkersCards = x.Data?.NumberOfWorkersCards,
                Phone = x.Data?.Phone,
                PIDNumber = x.Data?.PIDNumber,
                OrganisationTypeID = x.OrganisationTypeId,
                QualitySystems = x.Data.QualitySystems,
                Rating = x.Data?.Rating,
                SocialLaborAgreement = x.Data.SocialLaborAgreement,
                Status = x.Data?.Status,
                URL = x.Data?.URL,
            };
            return post;
        }
    }
}
