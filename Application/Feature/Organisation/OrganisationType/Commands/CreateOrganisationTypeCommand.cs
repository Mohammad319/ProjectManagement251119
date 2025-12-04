using Application.Interfaces;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.OrganisationType.Commands
{
    public sealed record CreateOrganisationTypeCommand(PostOrganisationTypeDTO dto) : IRequest<int>;

    public class CreateOrganisationTypeCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<CreateOrganisationTypeCommand, int>
    {
        public async Task<int> Handle(CreateOrganisationTypeCommand request, CancellationToken cancellationToken)
        {
            OrganisationTypeEntity OrganisationType = new() { Name = request.dto.Name, IsVisible = request.dto.IsVisible };
            dataAccess.OrganisationType.Add(OrganisationType);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return OrganisationType.Id;
        }
    }
}
