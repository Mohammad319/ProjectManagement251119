using Application.Interfaces;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.Organisation.Commands
{
    public sealed record CreateOrganisationCommand(PostOrganisationDTO dto) : IRequest<int>;
    public class CreateCompanyCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<CreateOrganisationCommand, int>
    {
        public async Task<int> Handle(CreateOrganisationCommand request, CancellationToken cancellationToken)
        {
            OrganisationEntity Org = new();
            request.dto.CopyPropertiesTo(Org);
            request.dto.CopyPropertiesTo(Org.Metadata);
            Org.OrganisationTypeId = request.dto.OrganisationTypeID;
            dataAccess.Organisation.Add(Org);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return Org.Id;
        }
    }
}
