using Application.Interfaces;
using System;

namespace Application.Feature.Folder.Commands
{
    public sealed record NewOrderFolderCommand(Guid Id, double NewOrder) : IRequest<bool>;

    public class NewOrderFolderCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<NewOrderFolderCommand, bool>
    {
        public async Task<bool> Handle(NewOrderFolderCommand request, CancellationToken cancellationToken)
        {
            var folder = await dataAccess.Folders.FindAsync(request.Id, cancellationToken);
            if (folder == null)
                return false;

            folder.SortOrder = request.NewOrder;

            dataAccess.Folders.Update(folder);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
