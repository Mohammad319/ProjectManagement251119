using Application.Interfaces;
using Application.Interfaces.Context;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Organisation.OrganisationCategory.Commands
{
    public sealed record DeleteOrganisationCategoryCommand(int Id) : IRequest<bool>;
        public class DeleteOrganisationCategoryCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<DeleteOrganisationCategoryCommand, bool>
        {
            public async Task<bool> Handle(DeleteOrganisationCategoryCommand request, CancellationToken cancellationToken)
            {
                var OfferCategory = await dataAccess.OrganisationCategory.FindAsync(request.Id);
                if (OfferCategory == null)
                    return false;
            if (await dataAccess.OrganisationCategory.AnyAsync(x => x.ParentCategoryId == request.Id))
                return false;
                dataAccess.OrganisationCategory.Remove(OfferCategory);
                await dataAccess.SaveChangesAsync(cancellationToken);
                return true;
            }

        }
    }
