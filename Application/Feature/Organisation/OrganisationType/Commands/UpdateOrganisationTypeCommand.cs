using Application.Interfaces;
using Application.Interfaces.Context;
using ProjectManagement.Shared.DTO.Organisation;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Organisation.OrganisationType.Commands
{
    public sealed record UpdateOrganisationTypeCommand(PostOrganisationTypeDTO dto, int Id) : IRequest<bool>;

        public class UpdateCustomerGroupCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<UpdateOrganisationTypeCommand, bool>
        {
        public async Task<bool> Handle(UpdateOrganisationTypeCommand request, CancellationToken cancellationToken)
            {
                var CustomerGroup = await dataAccess.OrganisationType.FindAsync(request.Id, cancellationToken);
                if (CustomerGroup == null)
                    return false;
                CustomerGroup.Name = request.dto.Name;
                CustomerGroup.IsVisible = request.dto.IsVisible;
                dataAccess.OrganisationType.Update(CustomerGroup);
                await dataAccess.SaveChangesAsync(cancellationToken);
                return true;
            }
        }
    }
