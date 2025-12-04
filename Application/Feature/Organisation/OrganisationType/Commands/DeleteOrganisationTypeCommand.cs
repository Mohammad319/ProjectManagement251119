using Application.Interfaces;

namespace Application.Feature.Organisation.OrganisationType.Commands
{
    public sealed record DeleteOrganisationTypeCommand(int Id) : IRequest<bool>;
    public class DeleteOrganisationTypeCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<DeleteOrganisationTypeCommand, bool>
    {
        public async Task<bool> Handle(DeleteOrganisationTypeCommand request, CancellationToken cancellationToken)
        {
            var customerGroup = await dataAccess.OrganisationType.FindAsync(request.Id, cancellationToken);
            if (customerGroup == null)
                return false;
            dataAccess.OrganisationType.Remove(customerGroup);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
