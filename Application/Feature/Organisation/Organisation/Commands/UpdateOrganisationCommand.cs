using Application.Interfaces;
using AutoMapper;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.Organisation.Commands
{
    public sealed record UpdateOrganisationCommand(PostOrganisationDTO dto, int Id) : IRequest<bool>;

    public class UpdateOrganisationCommandHandler(IShardingSingleDbContext dataAccess, IMapper mapper) : IRequestHandler<UpdateOrganisationCommand, bool>
    {
        public async Task<bool> Handle(UpdateOrganisationCommand request, CancellationToken cancellationToken)
        {
            var Company = await dataAccess.Organisation.FindAsync(request.Id);
            mapper.Map(request.dto, Company);
            Company.Metadata = request.dto.CopyPropertiesTo(Company.Metadata);
            dataAccess.Organisation.Update(Company);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
