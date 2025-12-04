using Application.Interfaces;

namespace Application.Feature.Calculation.ResourceType.Commands
{
    public sealed record DeleteResourceSortCommand(int Id) : IRequest<bool>;

    public class DeleteResourceSortCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<DeleteResourceSortCommand, bool>
    {
        public async Task<bool> Handle(DeleteResourceSortCommand request, CancellationToken cancellationToken)
        {
            var resourceSort = await dataAccess.ResourceSort.FindAsync(request.Id, cancellationToken);
            if (resourceSort == null)
                return false;
            dataAccess.ResourceSort.Remove(resourceSort);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
