using Application.Interfaces;

namespace Application.Feature.Calculation.ResourceType.Commands
{
    public sealed record DeleteResourceTypeCommand(int Id) : IRequest<bool>;

    public class DeleteResourceTypeCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<DeleteResourceTypeCommand, bool>
    {
        public async Task<bool> Handle(DeleteResourceTypeCommand request, CancellationToken cancellationToken)
        {
            var resourceType = await dataAccess.ResourceType.FindAsync(request.Id, cancellationToken);
            if (resourceType == null)
                return false;
            dataAccess.ResourceType.Remove(resourceType);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
