using Application.Interfaces;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.OrganisationCategory.Commands
{
    public sealed record UpdateOrganisationCategoryCommand(PutOrganisationCategoryDTO Dto) : IRequest<bool>;
    public class UpdateOrganisationCategoryCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<UpdateOrganisationCategoryCommand, bool>
    {
        public async Task<bool> Handle(UpdateOrganisationCategoryCommand request, CancellationToken cancellationToken)
        {
            var OfferCategory = await dataAccess.OrganisationCategory.FindAsync(request.Dto.Id, cancellationToken);
            if (OfferCategory == null)
                return false;
            OfferCategory.Name = request.Dto.Name;
            dataAccess.OrganisationCategory.Update(OfferCategory);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
