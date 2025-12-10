using Application.Interfaces;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Feature.Calculation.ResourceType.Commands
{
    public sealed record UpdateResourceSortCommand(PostResourceSortDTO Dto, int Id) : IRequest<bool>;

    public class UpdateResourceSortCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<UpdateResourceSortCommand, bool>
    {
        public async Task<bool> Handle(UpdateResourceSortCommand request, CancellationToken cancellationToken)
        {
            var resourceSort = await dataAccess.ResourceSorts.FindAsync(request.Id, cancellationToken);
            if (resourceSort == null)
                return false;

            resourceSort.Name = request.Dto.Name;
            resourceSort.IsVisible = request.Dto.IsVisible;
            request.Dto.CopyPropertiesTo(resourceSort.Metadata);
            resourceSort.AccountId = request.Dto.AccountId; 
            dataAccess.ResourceSorts.Update(resourceSort);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
