using Application.Interfaces;
using Application.Interfaces.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Organisation.Organisation.Commands
{
    public sealed record DeleteOrganisationCommand(int Id) : IRequest<bool>;
        public class DeleteOrganisationCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<DeleteOrganisationCommand, bool>
        {
            public async Task<bool> Handle(DeleteOrganisationCommand request, CancellationToken cancellationToken)
            {
                var Company = await dataAccess.Organisation.FindAsync(request.Id);
                if (Company == null)
                    return false;
                var offers = await dataAccess.Offers.Where(x => x.OrganisationId == Company.Id).ToListAsync(cancellationToken: cancellationToken);
                if (offers != null) foreach (var offer in offers) offer.OrganisationId = null;
                dataAccess.Offers.UpdateRange(offers);
                dataAccess.Organisation.Remove(Company);
                await dataAccess.SaveChangesAsync(cancellationToken);
                return true;
            }
        }
    }
